import { ImageResponse } from 'next/og'

// Configuration de l'image OG
export const runtime = 'edge'
export const alt = 'Partie de Padel sur Vibora'
export const size = {
  width: 1200,
  height: 630,
}
export const contentType = 'image/png'

// Fonction helper pour formater la date de manière "catchy"
function formatCatchyDate(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const gameDate = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  
  const diffDays = Math.floor((gameDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))
  const hour = date.getHours()
  const minutes = date.getMinutes().toString().padStart(2, '0')
  
  let timeOfDay = ''
  if (hour >= 6 && hour < 12) timeOfDay = 'matin'
  else if (hour >= 12 && hour < 18) timeOfDay = 'après-midi'
  else timeOfDay = 'soir'
  
  if (diffDays === 0) return `Aujourd'hui ${timeOfDay} · ${hour}h${minutes}`
  if (diffDays === 1) return `Demain ${timeOfDay} · ${hour}h${minutes}`
  if (diffDays <= 7) {
    const dayName = date.toLocaleDateString('fr-FR', { weekday: 'long' })
    return `${dayName.charAt(0).toUpperCase() + dayName.slice(1)} · ${hour}h${minutes}`
  }
  
  return date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' })
}

export default async function Image({ params }: { params: { token: string } }) {
  try {
    // Charger l'image de fond terrain de padel
    const baseUrl = process.env.NEXT_PUBLIC_APP_URL || 'http://localhost:3000'
    let backgroundImageData = ''
    
    try {
      const bgResponse = await fetch(`${baseUrl}/og-padel-background.jpg`)
      if (bgResponse.ok) {
        const buffer = await bgResponse.arrayBuffer()
        const base64 = Buffer.from(buffer).toString('base64')
        backgroundImageData = `data:image/jpeg;base64,${base64}`
      }
    } catch (err) {
      console.log('[OG Image] Could not load background image, using gradient fallback')
    }
    
    // Récupérer les métadonnées de la partie
    const apiUrl = process.env.NEXT_PUBLIC_VIBORA_API_URL || process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7293'
    const url = `${apiUrl}/shares/${params.token}/metadata`
    
    console.log('[OG Image] Fetching metadata from:', url)
    console.log('[OG Image] Token:', params.token)
    
    const response = await fetch(url, {
      headers: {
        'Accept': 'application/json',
      },
      cache: 'no-store', // Important pour avoir les données à jour
      // @ts-ignore - Nécessaire en dev pour accepter les certificats auto-signés
      ...(process.env.NODE_ENV === 'development' && {
        // Note: fetch() en Edge Runtime ne supporte pas rejectUnauthorized
        // Si ça ne marche pas, il faudra utiliser NEXT_PUBLIC_API_URL avec http
      })
    })
    
    console.log('[OG Image] Response status:', response.status)
    
    if (!response.ok) {
      const errorText = await response.text()
      console.error('[OG Image] API Error:', response.status, errorText)
      throw new Error(`API returned ${response.status}`)
    }
    
    const data = await response.json()
    console.log('[OG Image] Data received:', data)

    // Le backend retourne gameDateTime, pas dateTime
    const dateTime = data.gameDateTime || data.dateTime
    const skillLevel = data.skillLevel ? parseInt(data.skillLevel) : null
    
    const catchyDate = formatCatchyDate(dateTime)
    const spotsLeft = data.maxPlayers - data.currentPlayers
    const isFull = spotsLeft === 0

    // Format date/heure comme dans la card
    const date = new Date(dateTime)
    const dateFormatted = date.toLocaleDateString('fr-FR', { 
      weekday: 'long', 
      day: 'numeric', 
      month: 'long' 
    })
    const timeFormatted = `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`
    const endHour = date.getHours() + 1
    const endTimeFormatted = `${endHour.toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`

    return new ImageResponse(
      (
        <div
          style={{
            height: '100%',
            width: '100%',
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'space-between',
            padding: '50px 60px',
            backgroundImage: backgroundImageData 
              ? `linear-gradient(to top, rgba(0,0,0,0.95) 0%, rgba(0,0,0,0.6) 50%, rgba(0,0,0,0.2) 100%), url(${backgroundImageData})`
              : 'linear-gradient(to top, rgba(0,0,0,0.95) 0%, rgba(0,0,0,0.6) 50%, rgba(0,0,0,0.2) 100%), linear-gradient(135deg, #10b981 0%, #059669 100%)',
            backgroundSize: '100% 100%',
            backgroundPosition: 'center',
            backgroundRepeat: 'no-repeat',
            fontFamily: 'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
          }}
        >
            {/* Badge principal - Plus gros et plus visible */}
            <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
              <div
                style={{
                  background: isFull ? 'rgba(239, 68, 68, 0.95)' : 'rgba(34, 197, 94, 0.95)',
                  color: 'white',
                  padding: '16px 32px',
                  borderRadius: '12px',
                  fontSize: 36,
                  fontWeight: 'bold',
                  boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
                }}
              >
                {isFull ? '🔒 COMPLET' : `${spotsLeft} PLACE${spotsLeft > 1 ? 'S' : ''}`}
              </div>
              <div
                style={{
                  display: skillLevel ? 'flex' : 'none',
                  background: 'rgba(99, 102, 241, 0.95)',
                  color: 'white',
                  padding: '16px 32px',
                  borderRadius: '12px',
                  fontSize: 36,
                  fontWeight: 'bold',
                  boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
                }}
              >
                ⭐ NIV. {skillLevel || 0}
              </div>
            </div>

            {/* Titre et infos en bas - Plus gros pour WhatsApp */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
              {/* Titre lieu - TRÈS GROS */}
              <div
                style={{
                  fontSize: 72,
                  fontWeight: 'bold',
                  color: 'white',
                  lineHeight: 1.1,
                  textShadow: '0 4px 16px rgba(0,0,0,0.9)',
                }}
              >
                {data.location}
              </div>

              {/* Date et heure - Plus gros */}
              <div
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '8px',
                  fontSize: 38,
                  fontWeight: '600',
                  color: 'rgba(255,255,255,0.95)',
                  textShadow: '0 2px 8px rgba(0,0,0,0.8)',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  <span style={{ fontSize: 42 }}>📅</span>
                  <span>{dateFormatted}</span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  <span style={{ fontSize: 42 }}>🕐</span>
                  <span>{timeFormatted} - {endTimeFormatted}</span>
                </div>
              </div>

              {/* Logo Vibora en bas */}
              <div
                style={{
                  marginTop: '8px',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '16px',
                  fontSize: 40,
                  fontWeight: 'bold',
                  color: 'rgba(255,255,255,0.9)',
                }}
              >
                <span style={{ fontSize: 48 }}>🎾</span>
                <span>VIBORA</span>
              </div>
            </div>
        </div>
      ),
      size
    )
  } catch (error) {
    console.error('Error generating OG image:', error)
    
    // Fallback image en cas d'erreur
    return new ImageResponse(
      (
        <div
          style={{
            background: 'linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)',
            width: '100%',
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'white',
            fontFamily: 'system-ui, -apple-system, sans-serif',
          }}
        >
          <div style={{ fontSize: 72, marginBottom: 24 }}>🎾</div>
          <div style={{ fontSize: 64, fontWeight: 'bold' }}>Vibora</div>
          <div style={{ fontSize: 32, marginTop: 16, opacity: 0.9 }}>
            Trouve ta prochaine partie de Padel
          </div>
        </div>
      ),
      size
    )
  }
}
