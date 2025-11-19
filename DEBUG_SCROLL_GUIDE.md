# Debugging Scroll Position Issue - Guide

## Current Issue
When clicking a footnote link (Sura 17 Aya 44 → Sura 41 Aya 11), the back button doesn't return to Sura 17. Instead it scrolls within Sura 41.

## Changes Made

### 1. Fixed JavaScript Scroll Saving
- **Removed `history.replaceState()`** - This was modifying the browser history, causing back button issues
- **Dual Save Strategy**: Now saves position with BOTH the detected Aya ID AND the base URL
- **Enhanced Logging**: All scroll operations now log to console and on-screen overlay

### 2. Added Debug Overlay for Android

A **green bug button (🐛)** will appear in the top-right corner. Click it to see debug messages on screen!

The overlay shows:
- When scroll positions are saved
- What Aya ID was detected
- What keys are used for storage
- When positions are restored

## How to Debug on Android

### Method 1: On-Screen Debug Overlay (NEW!)
1. Look for the green bug icon (🐛) in top-right corner
2. Tap it to show/hide the debug overlay
3. The overlay shows the last 20 debug messages
4. Messages include timestamps

### Method 2: Chrome Remote Debugging (Traditional)
1. Enable USB Debugging on Android device
2. Connect device to PC via USB
3. Open Chrome on PC: `chrome://inspect`
4. Click "Inspect" under your device
5. View console logs in DevTools

### Method 3: Visual Studio Output Window
1. Run the app in Debug mode
2. Open View → Output
3. Select "Debug" from dropdown
4. See `Debug.WriteLine()` messages

## Testing the Fix

### Test Scenario:
1. Open Sura 17
2. Scroll to Aya 44
3. Click the footnote link (points to Sura 41 Aya 11)
4. **Expected**: Should navigate to Sura 41 Aya 11
5. Click back button (or Alt+Left Arrow on Windows)
6. **Expected**: Should return to Sura 17 Aya 44
7. Click back button again
8. **Expected**: Should return to Sura list

### What to Check in Debug Overlay:
When clicking the footnote link, you should see:
```
=== SAVING SCROLL POSITION ===
Current URL: /AyaList?SuraId=17
Top visible Aya ID: 44
Saved as: /AyaList?SuraId=17#44
Also saved as base: /AyaList?SuraId=17
```

When clicking back, you should see:
```
=== RESTORING SCROLL POSITION ===
Current URL: /AyaList?SuraId=17#44
Trying key with hash: /AyaList?SuraId=17#44 → [position]
Restoring to position: [position]
```

## Windows Back Button (Alt+Left Arrow)

The browser back button should work, but Alt+Left Arrow might not work in Blazor WebView. Instead:
- Use the browser's back button if visible
- Or add a custom back button in your app UI
- Or use mouse side buttons if available

## Key Files Modified

1. **wwwroot/js/site.js**
   - `saveScrollPosition()` - No longer modifies history
   - `getTopVisibleAyaId()` - Enhanced logging
   - `restoreScrollPosition()` - Enhanced logging
   - Added `createDebugOverlay()` and `debugLog()` for Android debugging

2. **Pages/AyaList.razor**
   - `OnLocationChanging()` - Properly awaits save call
   - Already has enhanced Debug.WriteLine statements

## Next Steps

1. **Deploy** the updated app to Android
2. **Look for** the green bug icon (🐛)
3. **Tap it** to open debug overlay
4. **Test** the scenario above
5. **Report** what messages you see in the overlay

## Common Issues & Solutions

### If back button still doesn't work:
- Check if Blazor is intercepting navigation
- Verify browser history has the correct entries
- Check if OnLocationChanging is being called multiple times

### If debug overlay doesn't appear:
- Check browser console for JavaScript errors
- Verify the script is being loaded (check Network tab)
- Try refreshing the page

### If positions aren't being saved:
- Verify `getTopVisibleAyaId()` returns a valid ID
- Check sessionStorage in browser DevTools
- Ensure Aya elements have valid `id` attributes

## Logging Key
- `===` sections indicate major operations
- Timestamps show when events occurred  
- `→` shows values retrieved from storage
- `Found X ayas` confirms DOM elements are present
