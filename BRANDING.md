# TürkiyeAI — Brand Guide

> AI-Powered Türkiye Travel Platform · Inspired by the beauty, culture, and colours of Türkiye

---

## Brand Name

| Usage | Value |
|---|---|
| **Full product name** | TürkiyeAI |
| **Short / app name** | TürkiyeAI |
| **Domain slug** | turkiyeai |
| **Tagline** | Your AI Türkiye Travel Expert |

---

## Logo Assets

All logo files live in `src/TurkAI.Web/wwwroot/images/`.

| File | Usage |
|---|---|
| `logo-icon.svg` | App icon, avatar, favicon source, small placements |
| `logo.svg` | Full wordmark — use on light / white backgrounds |
| `logo-white.svg` | Full wordmark — use on dark / coloured backgrounds (sidebar, hero) |
| `banner-hero.svg` | Hero section background with Turkish tile pattern |
| `favicon.svg` | Browser tab / PWA icon (SVG, 32 × 32 base) |

### Logo Construction

The icon mark is a circular badge featuring:
- **Turkish flag motif** — white crescent moon and 5-pointed star on a deep-crimson gradient background
- **Ottoman seal ring** — subtle dashed inner circle, referencing historical tugra seals
- **Red gradient** — bright crimson (#E0244A) at top-left to deep carmine (#8B0015) at bottom-right

The wordmark renders **"Türkiye"** in Turkish Red and **"AI"** in Aegean Blue with a light-grey tagline beneath.

### Clear Space

Maintain clear space equal to **the height of the crescent** on all sides of the logo.

### Minimum Sizes

| Context | Minimum height |
|---|---|
| Digital — icon only | 24 px |
| Digital — full wordmark | 36 px |
| Print | 10 mm |

### Don'ts

- Do not recolour the logo icon
- Do not stretch or skew the mark
- Do not place the colour logo on a busy photographic background (use `logo-white.svg` instead)
- Do not add drop shadows or filters beyond those built into the SVG

---

## Colour Palette

Defined as CSS custom properties in `src/TurkAI.Web/wwwroot/brand.css`.

### Primary — Turkish Flag

| Token | Hex | Name | Reference |
|---|---|---|---|
| `--brand-red` | `#C8102E` | Atatürk Red | Turkish national flag |
| `--brand-red-light` | `#E8315A` | Bright Crimson | Hover / active states |
| `--brand-red-dark` | `#A0001E` | Deep Carmine | Button borders, pressed states |
| `--brand-white` | `#FFFFFF` | White | Flag white, text on red |

### Secondary — Aegean & Mediterranean

| Token | Hex | Name | Reference |
|---|---|---|---|
| `--brand-aegean` | `#1A6B9A` | Aegean Blue | Deep Aegean Sea off Bodrum |
| `--brand-turquoise` | `#1E9ABD` | İznik Turquoise | İznik tile glaze |
| `--brand-turquoise-light` | `#40BFCE` | Shallow Aegean | Turquoise coastal water |
| `--brand-cobalt` | `#1255A4` | İznik Cobalt | Dark tile cobalt |
| `--brand-bosphorus` | `#0A2342` | Bosphorus Night | Night sky over Istanbul |

### Accent — Ottoman & Anatolian

| Token | Hex | Name | Reference |
|---|---|---|---|
| `--brand-gold` | `#D4A843` | Ottoman Gold | Mosaic tile gold, Topkapı gilding |
| `--brand-gold-light` | `#F0C96A` | Sunlit Gold | Warm gold highlights |
| `--brand-terracotta` | `#C0523B` | Terracotta | Mediterranean clay rooftops, pottery |
| `--brand-saffron` | `#E8942A` | Saffron | Grand Bazaar spice stalls |
| `--brand-rose` | `#C97B8E` | Cappadocian Rose | Pink volcanic rock, rose valleys |

### Warm Neutrals — Sand & Earth

| Token | Hex | Name | Reference |
|---|---|---|---|
| `--brand-sand` | `#F5E6C8` | Anatolian Sand | Aegean beach sand, limestone |
| `--brand-ivory` | `#FAF6EE` | Ottoman Parchment | Manuscript, fine paper |
| `--brand-stone` | `#8D7B6B` | Ancient Stone | Ephesus marble, carved columns |
| `--brand-earth` | `#6B4F3A` | Anatolian Soil | Rich terracotta earth |

### Dark Neutrals — Night & Depth

| Token | Hex | Name | Reference |
|---|---|---|---|
| `--brand-night` | `#0A1628` | Cappadocian Night | Starry night over valleys |
| `--brand-navy` | `#1C3A5E` | Aegean Evening | Sunset over the Aegean |

---

## Typography

| Role | Stack |
|---|---|
| Display / headings | `'Segoe UI', system-ui, -apple-system, sans-serif` |
| Body text | `'Helvetica Neue', Helvetica, Arial, sans-serif` |
| Code / mono | `'Cascadia Code', 'Fira Code', Consolas, monospace` |

**Heading weights:** 700 (h3–h6), 800 (h1–h2, display)  
**Body weight:** 400 regular, 500 medium for emphasis

---

## Hero Banner

The hero background (`banner-hero.svg`) layers:

1. **Gradient** — Turkish Red → deep carmine → Aegean Navy → Bosphorus Night
2. **Geometric tile pattern** — diamond-grid motif referencing İznik ceramic tile layouts
3. **Decorative crescent** — large semi-transparent crescent on the right side
4. **Decorative star** — matching 5-pointed star geometry
5. **Ottoman gold accent strip** — a CSS `::after` pseudo-element at the base of the hero

---

## Sidebar / Navigation

The sidebar uses a 4-stop gradient:

```
Turkish Red   #C8102E   0%
Deep Carmine  #8B2040  35%
Aegean Navy   #1C3A5E  70%
Bosphorus     #0A1628 100%
```

This mirrors the journey from the warmth of Anatolia down to the depth of the Aegean.

---

## Voice & Tone

- **Warm** — like a knowledgeable local guide, not a corporate chatbot
- **Enthusiastic** — Türkiye's richness deserves genuine excitement
- **Precise** — travellers rely on accurate, actionable information
- **Bilingual** — always honour the Turkish language; use correct diacritics (ü, ş, ğ, ı, ö, ç)

---

## Cultural References Used in Branding

| Element | Cultural Source |
|---|---|
| Crescent & star | Turkish national flag (adopted 1936) |
| Ottoman gold | Topkapı Palace gilding, manuscript illumination |
| İznik turquoise | 16th-century İznik ceramic tile tradition |
| Diamond tile pattern | Islamic geometric art, Ottoman architecture |
| Terracotta | Aegean coastal villages, Bodrum earthenware |
| Saffron | Grand Bazaar spice trade |
| Cappadocian rose | Pink volcanic tuff of the Göreme valley |
| Aegean blue | Bodrum, Ölüdeniz, the Turquoise Coast |

---

*Brand assets © TürkiyeAI. All cultural references are used with respect and appreciation for Turkish heritage.*
