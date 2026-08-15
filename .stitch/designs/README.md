# Stitch Design System Export

**Project:** Plataforma AutoTransparência (`17038144535006921641`)  
**Screen:** Design System (`asset-stub-assets_96556faa25c94a73a3b4f741a34ae8f7`)

## Download status

Stitch API was attempted via:

```bash
curl "https://stitch.googleapis.com/v1/projects/17038144535006921641/screens/asset-stub-assets_96556faa25c94a73a3b4f741a34ae8f7"
```

Result: **401 UNAUTHENTICATED** — credentials required (OAuth 2 access token).

No Stitch MCP server is configured in this workspace.

## Gap

HTML and PNG assets were **not** downloaded. Design tokens in `packages/ui` use a minimal inferred set aligned with the AutoTransparência theme (trust, clarity, automotive catalog).

## To complete the export

1. Authenticate with Google Stitch (`gcloud auth login` or Stitch API key).
2. Call `get_screen` for the screen ID above.
3. Download HTML and PNG from the returned `downloadUrl` fields.
4. Save as `design-system.html` and `design-system.png` in this folder.
5. Re-extract tokens into `packages/ui/src/tokens/tokens.css` and `packages/ui/tailwind.preset.ts`.
