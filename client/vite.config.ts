import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
	plugins: [react()],
	server: {
		port: 5173,
		proxy: {
			"/api": {
				target: "http://server:8080", // target .NET
				changeOrigin: true,
			},
			"/hubs": {
				target: "http://server:8080",
				changeOrigin: true,
				ws: true, // forward the WebSocket upgrade
			},
		},

	},
});
