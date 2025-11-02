# Vibora Design System

Ce design system a été extrait des pages `app/page.tsx` et `app/create-game/page.tsx` pour maintenir **exactement** le même look & feel dans toute l'application.

## 🎯 Objectif

Pouvoir créer de nouvelles pages qui ressemblent **EXACTEMENT** aux pages existantes en utilisant des composants réutilisables standardisés.

## 📦 Composants créés

### 🎨 Animations (`@/lib/animation-variants`)

```tsx
import { 
  FADE_IN_ANIMATION_VARIANTS,
  STAGGER_CONTAINER_VARIANTS,
  SLIDE_UP_VARIANTS,
  SCALE_VARIANTS,
  SUBTLE_SCALE_VARIANTS
} from "@/lib/animation-variants"
```

Variants d'animation standardisés utilisés partout dans l'app.

### 🃏 Game Cards (`@/components/ui/game-card`)

```tsx
import { 
  GameCard, 
  UpcomingGameCard, 
  AvailableGameCard, 
  CreateGameCard 
} from "@/components/ui"

// Card de partie à venir avec image
<UpcomingGameCard
  id="1"
  title="Le Padel Club"
  subtitle="Court Central"
  time="18:00"
  date="Aujourd'hui"
  imageUrl="/vibrant-padel-match.png"
  players={["avatar1.jpg", "avatar2.jpg"]}
  playersNeeded={1}
/>

// Card de partie disponible
<AvailableGameCard
  id="2"
  title="Urban Padel"
  time="19:30"
  distance="4 km"
  spotsLeft={1}
  skillLevel={9}
  price={12}
/>

// Card de création de partie
<CreateGameCard
  title="Créer une partie"
  subtitle="et inviter des amis"
/>
```

### 🏷️ Filter Chips (`@/components/ui/filter-chip`)

```tsx
import { QuickFilters } from "@/components/ui"

const filters = [
  { id: "tonight", label: "Ce soir", count: 5, active: false },
  { id: "level", label: "Niveau 5-6", count: 8, active: true },
]

<QuickFilters 
  filters={filters}
  onFilterToggle={(filterId) => console.log(filterId)}
/>
```

### 📋 Section Headers (`@/components/ui/section-header`)

```tsx
import { SectionHeader, PageHeader } from "@/components/ui"

// Header de section avec lien "Voir tout"
<SectionHeader
  title="Prochaines parties"
  actionLabel="Voir tout"
  actionHref="/my-games"
/>

// Header de page avec action
<PageHeader
  title="Créer une partie"
  actionLabel="Modifier"
  onActionClick={() => setStep("preferences")}
/>
```

### 🦴 Skeleton Components (`@/components/ui/skeleton-components`)

```tsx
import { 
  SkeletonHomePage, 
  SkeletonGameCard, 
  SkeletonAvailableGameCard 
} from "@/components/ui"

// Skeleton complet de la page d'accueil
<SkeletonHomePage />

// Skeletons individuels
<SkeletonGameCard />
<SkeletonAvailableGameCard />
```

### 🎯 Interactive Buttons (`@/components/ui/interactive-button`)

```tsx
import { 
  InteractiveButton,
  InteractiveIconButton,
  InteractiveFormButton,
  InteractiveAvatarButton
} from "@/components/ui"

// Bouton avec animation intégrée
<InteractiveButton animation="normal">
  Cliquez-moi
</InteractiveButton>

// Bouton d'icône (comme dans le header)
<InteractiveIconButton>
  <Search className="w-5 h-5" />
</InteractiveIconButton>

// Bouton de formulaire
<InteractiveFormButton>
  Rechercher
</InteractiveFormButton>

// Bouton avatar
<InteractiveAvatarButton>
  <Avatar>...</Avatar>
</InteractiveAvatarButton>
```

### 📝 Form Components (`@/components/ui/vibora-form`)

```tsx
import { 
  VForm, 
  VInput, 
  VSelect, 
  VButton,
  VTimeSlotGrid,
  VLevelSelector
} from "@/components/ui"

<VForm>
  <VInput
    label="Date"
    type="date"
    value={selectedDate}
    onChange={(e) => setSelectedDate(e.target.value)}
    required
  />
  
  <VSelect
    label="Distance max."
    value={maxDistance}
    onValueChange={setMaxDistance}
  >
    <SelectItem value="5">5 km</SelectItem>
    <SelectItem value="10">10 km</SelectItem>
  </VSelect>

  <VTimeSlotGrid
    options={timeSlotOptions}
    selectedIds={selectedTimeSlots}
    onToggle={toggleTimeSlot}
  />

  <VLevelSelector
    selectedLevel={selectedLevel}
    onLevelChange={setSelectedLevel}
  />

  <VButton size="form" variant="primary">
    Rechercher
  </VButton>
</VForm>
```

### 🏗️ Layout Components (`@/components/ui/vibora-layout`)

```tsx
import { 
  VPage, 
  VHeader, 
  VMain, 
  VContainer,
  VScrollContainer,
  VStack,
  VSection
} from "@/components/ui"

<VPage animate>
  <VHeader>
    <VContainer>
      {/* Header content */}
    </VContainer>
  </VHeader>
  
  <VMain>
    <VStack spacing="lg">
      <VSection animate>
        <SectionHeader 
          title="Prochaines parties"
          actionLabel="Voir tout"
          actionHref="/my-games"
        />
        
        <VScrollContainer enableSnap edgeToEdge>
          {games.map(game => (
            <UpcomingGameCard key={game.id} {...game} />
          ))}
        </VScrollContainer>
      </VSection>
    </VStack>
  </VMain>
</VPage>
```

## 🎨 Palette de couleurs

Le design system utilise les couleurs définies dans `globals.css` :

- **Primary**: Bleu `oklch(0.6048 0.2166 257.2136)`
- **Success**: Vert `oklch(0.9 0.2 115)` pour les éléments actifs
- **Background/Card**: Avec backdrop-blur pour l'effet vitré
- **Muted-foreground**: Pour les textes secondaires

### Classes principales :
- `bg-card/80 backdrop-blur-sm` - Cards avec effet vitré
- `border-border/50` - Borders subtils
- `text-muted-foreground` - Texte secondaire
- `bg-success text-success-foreground` - Éléments actifs/positifs

## 🎭 Animations

Toutes les animations utilisent Framer Motion avec des variants cohérents :

```tsx
// Entrée en fondu avec mouvement vers le haut
variants={FADE_IN_ANIMATION_VARIANTS}

// Container avec animation échelonnée des enfants
variants={STAGGER_CONTAINER_VARIANTS}

// Entrée avec effet de scale
variants={SLIDE_UP_VARIANTS}

// Interactions hover/tap
whileHover={{ scale: 1.05 }}
whileTap={{ scale: 0.95 }}
```

## 📐 Layout et espacement

- **Container**: `container` (défini dans globals.css avec padding-inline: 1.5rem)
- **Sections**: `space-y-8` entre sections principales
- **Groupes**: `space-y-4` dans les sections
- **Éléments**: `space-y-3` pour les petits groupes

### Scroll horizontal :
```tsx
<div className="-mx-6">  {/* Negative margin pour edge-to-edge */}
  <div className="flex gap-4 px-6 overflow-x-auto hide-scrollbar snap-x snap-mandatory">
    {/* Content */}
  </div>
</div>
```

## ✨ Exemple complet

Voici comment créer une nouvelle page qui ressemble exactement aux pages existantes :

```tsx
"use client"

import { motion } from "framer-motion"
import { 
  VPage, 
  VHeader, 
  VMain, 
  VContainer,
  VScrollContainer,
  VStack,
  VSection,
  SectionHeader,
  QuickFilters,
  UpcomingGameCard,
  AvailableGameCard,
  InteractiveIconButton,
  STAGGER_CONTAINER_VARIANTS
} from "@/components/ui"

export default function MyNewPage() {
  return (
    <VPage animate>
      <VHeader>
        <VContainer>
          <div className="flex items-center justify-between h-20">
            <h1 className="text-xl font-bold">Ma page</h1>
            <InteractiveIconButton>
              <Search className="w-5 h-5" />
            </InteractiveIconButton>
          </div>
        </VContainer>
      </VHeader>

      <VMain>
        <VStack spacing="lg">
          {/* Filters */}
          <VSection animate>
            <QuickFilters 
              filters={filters}
              onFilterToggle={handleFilterToggle}
            />
          </VSection>

          {/* Games section */}
          <VSection animate>
            <SectionHeader
              title="Parties disponibles"
              actionLabel="Voir tout"
              actionHref="/games"
            />
            
            <VScrollContainer enableSnap edgeToEdge>
              {games.map(game => (
                <UpcomingGameCard key={game.id} {...game} />
              ))}
            </VScrollContainer>
          </VSection>
        </VStack>
      </VMain>
    </VPage>
  )
}
```

Cette approche garantit que **toutes** les nouvelles pages auront exactement le même look & feel que les pages existantes, avec une cohérence parfaite dans les animations, les couleurs, l'espacement et les interactions.