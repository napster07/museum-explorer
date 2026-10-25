import type { Metadata } from "next";
import "./globals.css";
export const metadata: Metadata = { title: "Museum Explorer — Follow your curiosity", description: "An independent museum experience prototype. Discover exhibitions and plan a visit." };
export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en-NZ"><body>{children}</body></html>;
}
