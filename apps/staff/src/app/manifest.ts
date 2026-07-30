import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "AtiqSalon Staff",
    short_name: "Salon Staff",
    description: "Assigned schedules and salon operations.",
    start_url: "/",
    scope: "/",
    display: "standalone",
    background_color: "#102520",
    theme_color: "#102520",
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
