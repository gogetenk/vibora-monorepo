# Setup .env.local for Vibora
$envContent = @"
NEXT_PUBLIC_SUPABASE_URL=https://qkzwstgzvlwnjnglkbio.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InFrendzdGd6dmx3bmpuZ2xrYmlvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjEzMzkxMTAsImV4cCI6MjA3NjkxNTExMH0.NqbszyufoU3U6HdlfN5FUEAlNbpp6vuMpQqOVv8TP_0
NEXT_PUBLIC_VIBORA_API_URL=http://localhost:5000
"@

$envContent | Out-File -FilePath ".env.local" -Encoding utf8
Write-Host "✅ .env.local created successfully!"
