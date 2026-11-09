import { expect, test } from '@playwright/test'

test('home page renders with Finnish default and switches to English', async ({ page }) => {
  await page.goto('/')

  const heading = page.getByRole('heading', { level: 1 })
  await expect(heading).toHaveText('Kryptoveroraportit')

  const fi = page.getByRole('button', { name: 'FI' })
  const en = page.getByRole('button', { name: 'EN' })
  await expect(fi).toHaveClass(/active/)

  await en.click()
  await expect(heading).toHaveText('Crypto Tax Reports')
  await expect(en).toHaveClass(/active/)
})

test.fixme('favicon no longer points at the Vite starter asset', async ({ page }) => {
  await page.goto('/')
  const icon = page.locator('link[rel="icon"]')
  await expect(icon).toHaveCount(1)
  // Blocked by open issue #6 (Vite branding). Remove fixme once the approved logo ships.
  await expect(icon).not.toHaveAttribute('href', '/vite.svg')
})
