import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // output: "standalone",
output: 'standalone',
  /* config options here */
  images: {
    unoptimized: true,
  },
  trailingSlash: true,
};

export default nextConfig;
