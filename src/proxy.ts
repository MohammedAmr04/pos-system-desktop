import createMiddleware from "next-intl/middleware";

export default createMiddleware({
  locales: ["ar"],
  defaultLocale: "ar",
  localePrefix: "as-needed",
});

export const config = {
  matcher: ["/((?!api|_next|_vercel|.*\\..*).*)"],
};
