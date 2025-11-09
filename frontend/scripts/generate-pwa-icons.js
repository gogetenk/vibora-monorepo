/**
 * Generate PWA Icons from SVG
 *
 * This script generates all required PWA icons from the placeholder SVG.
 * Run with: node scripts/generate-pwa-icons.js
 *
 * Requirements: npm install sharp
 */

const sharp = require('sharp')
const fs = require('fs')
const path = require('path')

const INPUT_SVG = path.join(__dirname, '../public/icon-placeholder.svg')
const OUTPUT_DIR = path.join(__dirname, '../public')

const icons = [
  { output: 'icon-192.png', size: 192, description: 'PWA standard icon (192x192)' },
  { output: 'icon-512.png', size: 512, description: 'PWA standard icon (512x512)' },
  { output: 'icon-maskable-512.png', size: 512, description: 'Android maskable/adaptive icon' },
  { output: 'apple-touch-icon.png', size: 180, description: 'iOS home screen icon' },
]

async function generateIcons() {
  console.log('🎨 Generating PWA icons from placeholder SVG...\n')

  // Check if input SVG exists
  if (!fs.existsSync(INPUT_SVG)) {
    console.error('❌ Error: icon-placeholder.svg not found in /public/')
    process.exit(1)
  }

  let successCount = 0
  let errorCount = 0

  for (const { output, size, description } of icons) {
    const outputPath = path.join(OUTPUT_DIR, output)

    try {
      await sharp(INPUT_SVG)
        .resize(size, size, {
          fit: 'contain',
          background: { r: 16, g: 185, b: 129, alpha: 1 }, // #10b981
        })
        .png()
        .toFile(outputPath)

      console.log(`✅ ${output} - ${description}`)
      successCount++
    } catch (error) {
      console.error(`❌ Failed to generate ${output}:`, error.message)
      errorCount++
    }
  }

  console.log(`\n📊 Summary: ${successCount} icons generated, ${errorCount} errors`)

  if (errorCount === 0) {
    console.log('\n🎉 All PWA icons generated successfully!')
    console.log('ℹ️  Note: These are PLACEHOLDER icons. Replace with final assets from designer.')
  } else {
    console.error('\n⚠️  Some icons failed to generate. Check errors above.')
    process.exit(1)
  }
}

// Run generator
generateIcons().catch((error) => {
  console.error('❌ Fatal error:', error)
  process.exit(1)
})
