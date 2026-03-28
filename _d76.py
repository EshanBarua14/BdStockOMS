path = 'BdStockOMS.Client/src/pages/AdminSettingsPage.tsx'
src  = open(path, encoding='utf-8').read()

# Fix 1: already done - BASE URL

# Fix 2: Fees - wire to API
if "apiFetch('/admin/fees').then(setFees)" not in src:
    src = src.replace(
        "  const [fees, setFees] = useState([",
        "  useEffect(() => { apiFetch('/admin/fees').then(setFees).catch(() => {}); }, []);\n  const [fees, setFees] = useState(["
    )
    print('OK fees')
else:
    print('SKIP fees')

# Fix 3: API Keys - wire to API
if "apiFetch('/admin/api-keys').then(setKeys)" not in src:
    src = src.replace(
        "  const [keys, setKeys] = useState([",
        "  useEffect(() => { apiFetch('/admin/api-keys').then(setKeys).catch(() => {}); }, []);\n  const [keys, setKeys] = useState(["
    )
    print('OK api-keys')
else:
    print('SKIP api-keys')

# Fix 4: Announcements - wire to API
if "apiFetch('/admin/announcements').then(setItems)" not in src:
    src = src.replace(
        "  const [items, setItems] = useState([",
        "  useEffect(() => { apiFetch('/admin/announcements').then(setItems).catch(() => {}); }, []);\n  const [items, setItems] = useState(["
    )
    print('OK announcements')
else:
    print('SKIP announcements')

# Fix 5: IP Whitelist - wire to API
if "apiFetch('/admin/ip-whitelist').then(setIps)" not in src:
    src = src.replace(
        "  const [ips, setIps] = useState([",
        "  useEffect(() => { apiFetch('/admin/ip-whitelist').then(setIps).catch(() => {}); }, []);\n  const [ips, setIps] = useState(["
    )
    print('OK ip-whitelist')
else:
    print('SKIP ip-whitelist')

# Fix 6: Backup history - wire to API
if "apiFetch('/admin/backup/history').then" not in src:
    src = src.replace(
        "  const [history] = useState([",
        "  const [history, setHistory] = useState([]);\n  useEffect(() => { apiFetch('/admin/backup/history').then(setHistory).catch(() => {}); }, []);\n  const [_history_unused] = useState(["
    )
    print('OK backup-history')
else:
    print('SKIP backup-history')

# Fix 7: Health - wire to API
if "apiFetch('/admin/health').then" not in src:
    src = src.replace(
        "  const [health, setHealth] = useState({",
        "  useEffect(() => { apiFetch('/admin/health').then(setHealth).catch(() => {}); }, []);\n  const [health, setHealth] = useState({"
    )
    print('OK health')
else:
    print('SKIP health')

# Fix 8: Roles - wire to API  
if "apiFetch('/admin/roles').then" not in src:
    src = src.replace(
        "  const [roles, _setRoles] = useState([",
        "  const [roles, setRoles] = useState([]);\n  useEffect(() => { apiFetch('/admin/roles').then(setRoles).catch(() => {}); }, []);\n  const [_roles_unused, _setRoles] = useState(["
    )
    print('OK roles')
else:
    print('SKIP roles')

open(path, 'w', encoding='utf-8').write(src)
print('Done - saved')
