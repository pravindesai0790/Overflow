import { cloudinary } from "@/lib/cloudinary";

// code to sign an image upload in cloudinary from nextjs 
export async function POST(request: Request) {
    const body = (await request.json()) as { paramsToSign: Record<string, string> };
    const {paramsToSign} = body
    
    const signature = cloudinary.v2.utils.api_sign_request(paramsToSign,
        process.env.CLOUDINARY_API_SECRET as string);
    
    return Response.json({signature});
}