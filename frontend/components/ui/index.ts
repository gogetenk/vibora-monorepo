/**
 * Vibora UI Components Export Index
 * 
 * This file provides easy imports for all custom Vibora UI components
 * that maintain the exact look and feel from the existing pages.
 */

// Animation variants
export * from "@/lib/animation-variants"

// Game cards
export {
  GameCard,
  UpcomingGameCard,
  AvailableGameCard,
  CreateGameCard,
} from "./game-card"

// Filter components
export {
  FilterChip,
  FilterChipGroup,
  QuickFilters,
  CompactFilters,
} from "./filter-chip"

// Section headers
export {
  SectionHeader,
  PageHeader,
  CompactSectionHeader,
  CenterSectionHeader,
} from "./section-header"

// Skeleton components
export {
  SkeletonCard,
  SkeletonAvatar,
  SkeletonText,
  SkeletonGameCard,
  SkeletonAvailableGameCard,
  SkeletonFilterChip,
  SkeletonSectionHeader,
  SkeletonInput,
  SkeletonButton,
  SkeletonPageHeader,
  SkeletonHomePage,
} from "./skeleton-components"

// Interactive buttons
export {
  InteractiveButton,
  InteractiveIconButton,
  InteractivePrimaryButton,
  InteractiveFormButton,
  InteractiveAvatarButton,
} from "./interactive-button"

// Form components
export {
  VInput,
  VSelect,
  VButton,
  VForm,
  VFormField,
  VFormSection,
  VTimeSlotGrid,
  VLevelSelector,
} from "./vibora-form"

// Layout components
export {
  VContainer,
  VPage,
  VSection,
  VStack,
  VScrollContainer,
  VGrid,
  VHeader,
  VMain,
  VContentCard,
} from "./vibora-layout"

// Re-export existing shadcn components for convenience
export { Button } from "./button"
export { Card, CardContent, CardHeader, CardFooter, CardTitle, CardDescription } from "./card"
export { Avatar, AvatarImage, AvatarFallback } from "./avatar"
export { Input } from "./input"
export { Label } from "./label"
export { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "./select"
export { Badge } from "./badge"
export { Tabs, TabsContent, TabsList, TabsTrigger } from "./tabs"