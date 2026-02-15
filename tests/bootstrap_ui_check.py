#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
app = (root / 'Components' / 'App.razor').read_text(encoding='utf-8', errors='ignore')
checks = {
    'bootstrap_css_latest': 'bootstrap@latest/dist/css/bootstrap.rtl.min.css' in app,
    'bootstrap_js_latest': 'bootstrap@latest/dist/js/bootstrap.bundle.min.js' in app,
    'theme_css': '/css/bootstrap-theme.css' in app,
    'rtl_theme_css': '/css/rtl-bootstrap-theme.css' in app,
}
file_checks = {
    'legacy_bootstrap_css_deleted': not (root / 'wwwroot' / 'bootstrap' / 'bootstrap.min.css').exists(),
    'legacy_bootstrap_map_deleted': not (root / 'wwwroot' / 'bootstrap' / 'bootstrap.min.css.map').exists(),
    'legacy_app_css_deleted': not (root / 'wwwroot' / 'css' / 'app.css').exists(),
}
failed = [name for name, ok in {**checks, **file_checks}.items() if not ok]
if failed:
    print('Bootstrap UI checks failed:')
    for f in failed:
        print('-', f)
    sys.exit(1)
print('Bootstrap UI checks passed.')
