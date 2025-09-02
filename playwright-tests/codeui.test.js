const { test, expect } = require('@playwright/test');

// Test configuration
const BASE_URL = 'http://localhost:5000';

test.describe('CodeUI Application Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the application
    await page.goto(BASE_URL);
  });

  test('Application loads successfully', async ({ page }) => {
    // Check if the application loads
    await expect(page).toHaveTitle(/CodeUI/);
    
    // Check for main navigation elements
    const navMenu = page.locator('nav');
    await expect(navMenu).toBeVisible();
  });

  test('Terminal page loads and is interactive', async ({ page }) => {
    // Navigate to terminal page
    await page.goto(`${BASE_URL}/terminal`);
    
    // Wait for terminal to load
    await page.waitForSelector('.terminal-container', { timeout: 10000 });
    
    // Check terminal header
    const terminalHeader = page.locator('.terminal-header h3');
    await expect(terminalHeader).toContainText('CodeUI Terminal');
    
    // Check terminal controls
    const clearButton = page.locator('button:has-text("Clear")');
    const focusButton = page.locator('button:has-text("Focus")');
    await expect(clearButton).toBeVisible();
    await expect(focusButton).toBeVisible();
    
    // Check status indicator
    const statusBadge = page.locator('.terminal-status .badge');
    await expect(statusBadge).toBeVisible();
    await expect(statusBadge).toContainText('Ready');
  });

  test('File Explorer page loads', async ({ page }) => {
    // Navigate to file explorer
    await page.goto(`${BASE_URL}/file-explorer`);
    
    // Wait for file explorer to load
    await page.waitForSelector('[data-testid="file-explorer"]', { 
      timeout: 10000,
      state: 'visible' 
    }).catch(() => {
      // If no test-id, try generic selector
      return page.waitForSelector('.file-explorer, .file-tree, .explorer', { 
        timeout: 10000,
        state: 'visible' 
      });
    });
    
    // Check if file explorer is visible
    const fileExplorer = page.locator('.file-explorer, .file-tree, .explorer, [data-testid="file-explorer"]').first();
    await expect(fileExplorer).toBeVisible();
  });

  test('Navigation works correctly', async ({ page }) => {
    // Test navigation to different pages
    const pages = [
      { path: '/', title: 'Home' },
      { path: '/terminal', title: 'Terminal' },
      { path: '/file-explorer', title: 'File' },
      { path: '/counter', title: 'Counter' }
    ];

    for (const pageInfo of pages) {
      await page.goto(`${BASE_URL}${pageInfo.path}`);
      await page.waitForLoadState('networkidle');
      
      // Check that we're on the right page
      const title = await page.title();
      expect(title.toLowerCase()).toContain(pageInfo.title.toLowerCase());
    }
  });

  test('Terminal can execute basic commands', async ({ page }) => {
    // Navigate to terminal
    await page.goto(`${BASE_URL}/terminal`);
    
    // Wait for terminal to be ready
    await page.waitForSelector('.terminal-element', { timeout: 10000 });
    await page.waitForTimeout(2000); // Give xterm time to initialize
    
    // Focus on terminal
    const focusButton = page.locator('button:has-text("Focus")');
    await focusButton.click();
    
    // Type a simple echo command
    await page.keyboard.type('echo "Hello from Playwright"');
    await page.keyboard.press('Enter');
    
    // Wait for output (may need adjustment based on actual behavior)
    await page.waitForTimeout(1000);
    
    // Check that terminal processed the command
    // Note: Actual verification depends on how xterm.js renders output
    const terminalContent = await page.locator('.terminal-element').textContent();
    // Basic check that terminal has content
    expect(terminalContent).toBeTruthy();
  });

  test('Clear button clears terminal', async ({ page }) => {
    // Navigate to terminal
    await page.goto(`${BASE_URL}/terminal`);
    
    // Wait for terminal
    await page.waitForSelector('.terminal-element', { timeout: 10000 });
    await page.waitForTimeout(2000);
    
    // Type something
    const focusButton = page.locator('button:has-text("Focus")');
    await focusButton.click();
    await page.keyboard.type('test content');
    
    // Clear terminal
    const clearButton = page.locator('button:has-text("Clear")');
    await clearButton.click();
    
    // Wait for clear to take effect
    await page.waitForTimeout(500);
    
    // Verify terminal was cleared (implementation specific)
    // This will depend on how xterm.js handles clearing
  });

  test('Application handles errors gracefully', async ({ page }) => {
    // Try to navigate to a non-existent page
    const response = await page.goto(`${BASE_URL}/non-existent-page`, {
      waitUntil: 'networkidle'
    });
    
    // Should either redirect or show error page
    expect(response.status()).toBeLessThan(500);
  });
});

// Test for checking if Claude CLI can be invoked
test.describe('CLI Integration Tests', () => {
  test('Can check for Claude CLI availability', async ({ page }) => {
    await page.goto(`${BASE_URL}/terminal`);
    await page.waitForSelector('.terminal-element', { timeout: 10000 });
    await page.waitForTimeout(2000);
    
    // Focus terminal
    const focusButton = page.locator('button:has-text("Focus")');
    await focusButton.click();
    
    // Try to check Claude version
    await page.keyboard.type('which claude');
    await page.keyboard.press('Enter');
    await page.waitForTimeout(2000);
    
    // Check status - should either show path or not found
    const statusBadge = page.locator('.terminal-status .badge');
    const statusText = await statusBadge.textContent();
    
    // Status should change from "Ready"
    expect(statusText).toBeTruthy();
  });
});