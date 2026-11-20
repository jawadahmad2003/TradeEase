$src = "C:\Users\Abdul\OneDrive\Desktop\TradeEase-Frontend\TradeEase-Frontend\tradeease"
$dest = "D:\Trade Ease\Trade Ease\tradeease-client"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$exclude = @('node_modules', '.vite', 'dist')
Get-ChildItem $src -Force | Where-Object { $exclude -notcontains $_.Name } | ForEach-Object {
  Copy-Item $_.FullName -Destination $dest -Recurse -Force
}
Write-Output "DONE: $((Get-ChildItem "$dest\src" -Recurse -File).Count) files in src"
