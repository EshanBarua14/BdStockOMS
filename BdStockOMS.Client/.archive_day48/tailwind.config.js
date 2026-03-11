/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: ['class', '[data-theme="obsidian"]'],
  theme: {
    extend: {
      fontFamily: {
        ui:      ['"Outfit"',        'system-ui', 'sans-serif'],
        display: ['"Space Grotesk"', 'system-ui', 'sans-serif'],
        mono:    ['"Space Mono"',    'monospace'],
      },
    },
  },
  plugins: [],
}
