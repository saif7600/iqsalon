import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "AtiqSalon Customer",
    short_name: "AtiqSalon",
    description: "Customer appointments and salon services.",
    start_url: "/",
    scope: "/",
    display: "standalone",
    background_color: "#f2eee5",
    theme_color: "#f2eee5",
    orientation: "portrait",
    icons: [
      {
        src: "/icon.svg",
        sizes: "any",
        type: "image/svg+xml",
        purpose: "any",
      },
    ],
  };
}
