import React from 'react';
import { motion } from 'framer-motion';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { MapPin } from 'lucide-react';
import { VLocationInput } from "@/components/ui/vibora-form";

// Helper functions moved from the main page
const getLevelHelpText = (level: string) => {
    const levelMap: { [key: string]: string } = {
        "1": "Niveau 1: Débutant. Vous apprenez les règles de base.",
        "2": "Niveau 2: Débutant+. Vous commencez à faire des échanges.",
        "3": "Niveau 3: Intermédiaire-. Vous maîtrisez les coups de base.",
        "4": "Niveau 4: Intermédiaire. Vous êtes à l'aise en attaque et en défense.",
        "5": "Niveau 5: Intermédiaire+. Vous utilisez des tactiques et variez vos coups.",
        "6": "Niveau 6: Avancé. Vous avez un jeu solide et compétitif.",
        "7": "Niveau 7: Avancé+. Vous participez à des tournois.",
        "8": "Niveau 8: Elite. Vous êtes un joueur expert/professionnel.",
    };
    return levelMap[level] || "Sélectionnez un niveau pour voir la description.";
};

const getQuickTimeSlots = () => {
    const now = new Date();
    const isTooLateForToday = now.getHours() >= 21;
    return [
        { id: "today", label: "Aujourd'hui 18-21h", timeSlot: "evening", disabled: isTooLateForToday },
        { id: "tomorrow", label: "Demain 9-12h", timeSlot: "morning" },
        { id: "pick", label: "Choisir précis" },
    ];
};

const formatDateForDisplay = (dateString: string) => {
    if (!dateString) return "";
    const date = new Date(dateString);
    return date.toLocaleDateString("fr-FR", { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
};

const generateTimeOptions = () => {
    const options = [];
    for (let hour = 8; hour <= 22; hour++) {
        for (let minute of ["00", "30"]) {
            if (hour === 22 && minute === "30") continue;
            options.push(`${String(hour).padStart(2, '0')}:${minute}`);
        }
    }
    return options;
};

const timeOptions = generateTimeOptions();

// Animation variants
const container = {
    hidden: { opacity: 0 },
    show: {
        opacity: 1,
        transition: {
            staggerChildren: 0.1,
        },
    },
};

const item = {
    hidden: { y: 20, opacity: 0 },
    show: { y: 0, opacity: 1 },
};

interface CreateGamePreferencesProps {
    selectedLevel: string;
    setSelectedLevel: (level: string) => void;
    selectedDate: string;
    setSelectedDate: (date: string) => void;
    selectedTimeSlots: string[];
    handleQuickTimeSlot: (slot: any) => void;
    selectedLocation: string;
    setSelectedLocation: (location: string) => void;
    showExactTime: boolean;
    selectedExactTime: string;
    setSelectedExactTime: (time: string) => void;
    hasCourtToggle: boolean;
    setHasCourtToggle: (value: boolean) => void;
    isSearching: boolean;
    getDynamicCTAText: () => string;
    onNextStep: () => void;
    triggerHapticFeedback: () => void;
}

export const CreateGamePreferences: React.FC<CreateGamePreferencesProps> = ({
    selectedLevel,
    setSelectedLevel,
    selectedDate,
    setSelectedDate,
    selectedTimeSlots,
    handleQuickTimeSlot,
    selectedLocation,
    setSelectedLocation,
    showExactTime,
    selectedExactTime,
    setSelectedExactTime,
    hasCourtToggle,
    setHasCourtToggle,
    isSearching,
    getDynamicCTAText,
    onNextStep,
    triggerHapticFeedback
}) => {
    return (
        <>
            <CardHeader className="border-b border-border/50 py-3">
                <CardTitle className="text-lg">Quand souhaitez-vous jouer ?</CardTitle>
            </CardHeader>
            <CardContent className="p-6">
                <motion.div className="space-y-6" variants={container}>
                    {/* Date Selection */}
                    <motion.div className="space-y-3" variants={item}>
                        <div className="space-y-2">
                            <div className="text-sm text-muted-foreground mb-1">
                                {selectedDate && formatDateForDisplay(selectedDate)}
                            </div>
                            <Input
                                type="date"
                                id="date"
                                value={selectedDate}
                                onChange={(e) => setSelectedDate(e.target.value)}
                                className="rounded-xl"
                                min={new Date().toISOString().split("T")[0]}
                            />
                        </div>
                    </motion.div>

                    {/* Quick Time Slot Chips */}
                    <motion.div className="space-y-3" variants={item}>
                        <Label className="text-base font-medium">Créneau</Label>
                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                            {getQuickTimeSlots().map((slot) => (
                                <Button
                                    key={slot.id}
                                    variant={slot.timeSlot && selectedTimeSlots.includes(slot.timeSlot) ? "default" : "outline"}
                                    className={`text-sm px-4 py-2 rounded-lg transition-all ${
                                        slot.disabled ? 'opacity-50 cursor-not-allowed' : 'hover:scale-105'
                                    }`}
                                    onClick={() => {
                                        if (!slot.disabled) {
                                            triggerHapticFeedback()
                                            handleQuickTimeSlot(slot)
                                        }
                                    }}
                                    disabled={slot.disabled}
                                >
                                    <div className="font-medium text-sm">{slot.label}</div>
                                </Button>
                            ))}
                        </div>

                        {/* Exact Time Selection (shown when "Choisir précis" is selected) */}
                        {showExactTime && (
                            <div className="space-y-3 mt-4 p-4 bg-muted/50 rounded-xl">
                                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                                    <div>
                                        <Label className="text-sm font-medium mb-2 block">Date</Label>
                                        <Input
                                            type="date"
                                            value={selectedDate}
                                            onChange={(e) => setSelectedDate(e.target.value)}
                                            className="rounded-xl"
                                            min={new Date().toISOString().split("T")[0]}
                                        />
                                    </div>
                                    <div>
                                        <Label className="text-sm font-medium mb-2 block">Heure</Label>
                                        <Select value={selectedExactTime} onValueChange={setSelectedExactTime}>
                                            <SelectTrigger className="rounded-xl">
                                                <SelectValue placeholder="Sélectionner une heure" />
                                            </SelectTrigger>
                                            <SelectContent>
                                                {timeOptions.map((time) => (
                                                    <SelectItem key={time} value={time}>
                                                        {time}
                                                    </SelectItem>
                                                ))}
                                            </SelectContent>
                                        </Select>
                                    </div>
                                </div>
                            </div>
                        )}
                    </motion.div>

                    {/* Location Preference */}
                    <motion.div className="space-y-3" variants={item}>
                        <Label className="text-base font-medium">Lieu</Label>
                        {!selectedLocation ? (
                            <button
                                onClick={() => setSelectedLocation("Autour de moi (5 km)")}
                                className="w-full p-3 rounded-xl border border-border bg-card hover:bg-accent hover:border-primary/30 transition-all duration-200 text-left"
                            >
                                <div className="flex items-center gap-2">
                                    <MapPin className="h-4 w-4 text-primary" />
                                    <span className="text-muted-foreground">Autour de moi (5 km)</span>
                                </div>
                            </button>
                        ) : (
                            <VLocationInput value={selectedLocation} onChange={setSelectedLocation} />
                        )}
                    </motion.div>

                    {/* Toggle "J'ai déjà un court" */}
                    <motion.div className="space-y-3" variants={item}>
                        <div className="flex items-center space-x-2">
                            <input
                                type="checkbox"
                                id="has-court"
                                checked={hasCourtToggle}
                                onChange={(e) => setHasCourtToggle(e.target.checked)}
                                className="rounded border-border"
                            />
                            <Label htmlFor="has-court" className="text-sm font-medium cursor-pointer">
                                J'ai déjà un court
                            </Label>
                        </div>

                        {hasCourtToggle && (
                            <div className="space-y-3 p-4 bg-muted/50 rounded-xl">
                                <div>
                                    <Label className="text-sm font-medium mb-2 block">Club</Label>
                                    <Input
                                        placeholder="Nom du club ou terrain"
                                        className="rounded-xl"
                                    />
                                </div>
                                <div>
                                    <Label className="text-sm font-medium mb-2 block">Prix indicatif</Label>
                                    <Input
                                        placeholder="ex: 25€/personne"
                                        className="rounded-xl"
                                    />
                                </div>
                            </div>
                        )}
                    </motion.div>

                    {/* Level Selection */}
                    <motion.div className="space-y-3" variants={item}>
                        <Label className="text-base font-medium">Niveau</Label>
                        <div className="grid grid-cols-4 gap-2 sm:grid-cols-8">
                            {Array.from({ length: 8 }, (_, i) => {
                                const level = i + 1;
                                const isSelected = selectedLevel === String(level);
                                const isAdjacent = Math.abs(parseInt(selectedLevel) - level) === 1;
                                return (
                                    <Button
                                        key={level}
                                        variant={isSelected ? "default" : isAdjacent ? "default" : "outline"}
                                        className={`h-12 text-base font-medium transition-all hover:scale-105 ${
                                            isSelected
                                                ? "bg-primary text-primary-foreground shadow-sm"
                                                : isAdjacent
                                                    ? "bg-primary/20 text-primary border-primary/50 hover:bg-primary/30"
                                                    : "hover:bg-accent"
                                            }`}
                                        onClick={() => {
                                            triggerHapticFeedback();
                                            setSelectedLevel(String(level));
                                        }}
                                    >
                                        {level}
                                    </Button>
                                );
                            })}
                        </div>
                        {/* Contextual help text */}
                        <div className="text-sm text-muted-foreground mt-2">
                            {getLevelHelpText(selectedLevel)}
                        </div>
                    </motion.div>
                </motion.div>
            </CardContent>
            <CardFooter className="border-t border-border/50 bg-muted/30 p-6">
                <motion.div
                    className="w-full"
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.5 }}
                >
                    <Button
                        className="w-full rounded-lg py-3 text-sm shadow-md transition-transform hover:scale-[1.02] active:scale-[0.98]"
                        onClick={onNextStep}
                        disabled={isSearching}
                    >
                        {isSearching && (
                            <div className="animate-spin rounded-full h-4 w-4 border-2 border-current border-t-transparent mr-2" />
                        )}
                        {getDynamicCTAText()}
                    </Button>
                </motion.div>
            </CardFooter>
        </>
    );
};