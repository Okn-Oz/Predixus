/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
      },
      colors: {
        primary: '#2B7BE4',
        accent: '#14B8A6',
        'dark-bg': '#0B1220',
        'dark-card': '#111827',
        'dark-sidebar': '#0D1526',
        'dark-border': '#1F2937',
      },
    },
  },
  plugins: [],
}
