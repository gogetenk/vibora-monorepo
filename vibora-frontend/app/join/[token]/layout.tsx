import { Metadata } from 'next'

// Fonction helper pour formater la date de manière "catchy" (UX friendly)
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
  
  if (diffDays === 0) return `Aujourd'hui ${timeOfDay}`
  if (diffDays === 1) return `Demain ${timeOfDay}`
  if (diffDays <= 7) {
    const dayName = date.toLocaleDateString('fr-FR', { weekday: 'long' })
    return dayName.charAt(0).toUpperCase() + dayName.slice(1)
  }
  
  return date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long' })
}

// Génération des métadonnées dynamiques pour Open Graph
export async function generateMetadata({
  params,
}: {
  params: { token: string }
}): Promise<Metadata> {
  try {
    // Récupérer les métadonnées de la partie via l'API
    const apiUrl = process.env.NEXT_PUBLIC_VIBORA_API_URL || process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7293'
    const response = await fetch(`${apiUrl}/shares/${params.token}/metadata`)
    
    if (!response.ok) {
      return {
        title: 'Partie introuvable | Vibora',
        description: 'Cette partie de padel n\'existe pas ou n\'est plus disponible.',
      }
    }
    
    const data = await response.json()

    // Le backend retourne gameDateTime, pas dateTime
    const dateTime = data.gameDateTime || data.dateTime
    const skillLevel = data.skillLevel ? parseInt(data.skillLevel) : null

    // Formater la date de manière catchy (conforme UX doc)
    const catchyDate = formatCatchyDate(dateTime)
    const spotsLeft = data.maxPlayers - data.currentPlayers
    
    // Titre optimisé pour WhatsApp/Telegram (concis et catchy)
    // Format: "🎾 Padel Demain soir - Club XYZ"
    const title = `🎾 Padel ${catchyDate} - ${data.location}`
    
    // Description détaillée avec les infos clés (conforme UX ligne 30-34)
    // Format: "2/4 joueurs · Niveau 5 · Rejoins cette partie !"
    const playerInfo = `${data.currentPlayers}/${data.maxPlayers} joueurs`
    const levelInfo = skillLevel ? ` · Niveau ${skillLevel}` : ''
    const spotsInfo = spotsLeft > 0 
      ? ` · ${spotsLeft} place${spotsLeft > 1 ? 's' : ''} disponible${spotsLeft > 1 ? 's' : ''}`
      : ' · Partie complète'
    
    const description = `${playerInfo}${levelInfo}${spotsInfo} · Rejoins cette partie à ${data.location} !`

    // URL de base de l'application
    const baseUrl = process.env.NEXT_PUBLIC_APP_URL || 'https://vibora.app'
    const url = `${baseUrl}/join/${params.token}`

    return {
      title,
      description,
      openGraph: {
        title,
        description,
        url,
        siteName: 'Vibora',
        locale: 'fr_FR',
        type: 'website',
        images: [
          {
            // L'image OG dynamique sera générée par opengraph-image.tsx
            url: `${baseUrl}/join/${params.token}/opengraph-image`,
            width: 1200,
            height: 630,
            alt: `Partie de Padel - ${data.location}`,
          },
        ],
      },
      twitter: {
        card: 'summary_large_image',
        title,
        description,
        images: [`${baseUrl}/join/${params.token}/opengraph-image`],
      },
      // Métadonnées additionnelles pour le référencement
      alternates: {
        canonical: url,
      },
      robots: {
        index: true,
        follow: true,
      },
    }
  } catch (error) {
    console.error('Failed to generate metadata:', error)
    
    // Métadonnées par défaut en cas d'erreur
    return {
      title: 'Rejoindre une partie de Padel | Vibora',
      description: 'Trouve et rejoins des parties de padel près de chez toi avec Vibora.',
      openGraph: {
        title: '🎾 Vibora - Trouve ta partie de Padel',
        description: 'Organise et rejoins des parties de padel facilement.',
        type: 'website',
      },
    }
  }
}

export default function JoinTokenLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return <>{children}</>
}
