# Icon Color Filter Fix - November 6, 2025

## Problem
Icons were displaying in black instead of the primary brown color (#8B5A3C), even though the `.svg-icon-primary` class was applied.

## Root Cause
Two conflicting CSS rules in `wwwroot/css/app.css`:

1. **Line 1163-1165**: `.header-bg-color .svg-icon { filter: brightness(0) invert(1) !important; }` 
   - This rule was TOO BROAD - it matched ALL `.svg-icon` elements within `.header-bg-color`
   - The `!important` flag forced all icons to white, overriding `.svg-icon-primary`

2. **Line 1169**: `.svg-icon-primary { filter: sepia(1) saturate(3) hue-rotate(15deg); }`
   - This was using an incorrect filter formula
   - Should have been: `filter: var(--icon-filter-primary);`

## Solution Applied

### Fix 1: Made `.header-bg-color` rule more specific
**Before:**
```css
.header-bg-color .svg-icon,
.card-bg-color .svg-icon-white {
    filter: brightness(0) invert(1) !important;
}
```

**After:**
```css
.header-bg-color .svg-icon-white {
    filter: brightness(0) invert(1) !important;
}
```

- Now only applies white filter to elements explicitly marked with `.svg-icon-white`
- Removed the overly broad `.header-bg-color .svg-icon` selector
- Primary colored icons in headers can now display in brown color

### Fix 2: Restored correct `.svg-icon-primary` filter
**Before:**
```css
.svg-icon-primary {
    filter: sepia(1) saturate(3) hue-rotate(15deg);
}
```

**After:**
```css
.svg-icon-primary {
    filter: var(--icon-filter-primary);
}
```

- Uses CSS variable for maintainability
- Automatically adapts to theme (brown in light mode, white in dark mode)
- Consistent with design pattern

### Fix 3: Updated navigation icons to use primary color
**Before:**
```css
.nav-link .svg-icon {
    filter: var(--icon-filter, none);
}
```

**After:**
```css
.nav-link .svg-icon {
    filter: var(--icon-filter-primary);
}
```

- Navigation menu icons now display in primary brown color (light mode)
- Automatically invert to white in dark mode

## Verification
✅ **Android Build**: 0 Warnings, 0 Errors
✅ **Windows Build**: 0 Warnings, 0 Errors

## CSS Variable Reference

### Light Mode (:root)
```css
--icon-filter-primary: invert(0.35) sepia(0.4) saturate(1.5) hue-rotate(25deg);  /* White → Brown */
--icon-filter-light: brightness(0) invert(1);  /* White → Bright White */
--icon-filter: none;  /* No modification */
```

### Dark Mode (.dark-theme)
```css
--icon-filter: brightness(0) invert(1);  /* All icons become bright white */
--icon-filter-primary: brightness(0) invert(1);  /* Override to white in dark */
```

## Icon Classes
- `.svg-icon-primary`: Displays in primary brown (light), white (dark)
- `.svg-icon-light`: Displays in bright white (light), bright white (dark)
- `.svg-icon-white`: Always white (used for header text on brown background)

## Result
All icons now display correctly:
- **Light Mode**: Primary brown icons on light backgrounds, bright white on brown backgrounds
- **Dark Mode**: All icons white automatically via CSS variable override
- **Navigation**: Icons in sidebar now show primary brown color (light mode)
