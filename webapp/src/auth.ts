import NextAuth from "next-auth"
import Keycloak from "@auth/core/providers/keycloak"
import {apiConfig, authConfig} from "@/lib/config";

export const { handlers, signIn, signOut, auth } = NextAuth({
    providers: [Keycloak({
        authorization: {
            params: {scope: 'openid profile email'},
            url: `${authConfig.kcIssuer}/protocol/openid-connect/auth`
        },
        // this is because at this point this is Next.js server to server communication between our webapp and Keycloak, Our browser is not involved at this point 
        token: `${authConfig.kcInternal}/protocol/openid-connect/token` ,
        userinfo: `${authConfig.kcInternal}/protocol/openid-connect/userinfo` ,
    })],
    session: { strategy: "jwt"},
    callbacks: {
        // it is called on every page refresh, route or API call
        async jwt({token, account}) {
            const now = Math.floor(Date.now() / 1000);
            
            if(account && account.access_token && account.refresh_token) {
                const res = await fetch(apiConfig.baseUrl + '/profiles/me', {
                    headers: {
                        authorization: `Bearer ${account.access_token}`,
                    }
                })
                
                if (res.ok) {
                    token.user = await res.json();
                } else {
                    console.log('Failed to fetch user profile: ', await res.text());
                }
                
                token.accessToken = account.access_token;
                token.refreshToken = account.refresh_token;
                token.accessTokenExpires = now + account.expires_in!;
                token.error = undefined;
                return token;
            }
            
            if(token.accessTokenExpires && now < token.accessTokenExpires) return token; // token is valid so return it.
            
            // it will get new token from the refresh token
            try {
                const response = await fetch(`${authConfig.kcInternal}/protocol/openid-connect/token`, {
                    method: 'POST',
                    headers: {'Content-Type': 'application/x-www-form-urlencoded'},
                    body: new URLSearchParams({
                        grant_type: 'refresh_token',
                        client_id: authConfig.kcClientId,
                        client_secret: authConfig.kcSecret,
                        refresh_token: token.refreshToken as string
                    })
                })
                
                const refreshed = await response.json();
                
                if(!response.ok) {
                    console.log('Failed to refresh token', refreshed);
                    token.error = 'RefreshAccessTokenError';
                    return token;
                }
                
                token.accessToken = refreshed.access_token;
                token.refreshToken = refreshed.refresh_token;
                token.accessTokenExpires = now + refreshed.expires_in!;
                console.log('Token refresh successfully: ', refreshed);
                
            } catch (error) {
                console.log('Failed to refresh token', error);
                token.error = 'RefreshAccessTokenError';
            }
            
            return token;
        },
        async session({session, token}) {
            if(token.user) {
                session.user = token.user;
            }
            
            if(token.accessToken) {
                session.accessToken = token.accessToken;
            }
            
            if(token.accessTokenExpires) {
                session.expires = new Date(token.accessTokenExpires * 1000) as unknown as typeof session.expires;
            }
            return session;
        }
    }
})