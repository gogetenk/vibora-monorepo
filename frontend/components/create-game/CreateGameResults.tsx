"use client"

import React from 'react';
import { Calendar, Clock, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { GameMatchCard, type SearchCriteria } from '@/components/ui/game-match-card';

// InfoMessage component from original
const InfoMessage = ({ icon, title, message, buttonText, onButtonClick }: any) => (
  <div className="mb-6 p-4 bg-primary/10 border border-primary/20 rounded-lg">
    <div className="flex items-start">
      <div className="flex-shrink-0 mr-3">
        <div className="bg-primary/20 rounded-full p-1">{icon}</div>
      </div>
      <div className="flex-1">
        <h4 className="text-sm font-medium text-primary-foreground/90">{title}</h4>
        <p className="text-sm text-primary-foreground/80 mt-1">{message}</p>
        {buttonText && (
          <Button variant="default" size="sm" className="mt-3" onClick={onButtonClick}>
            {buttonText}
          </Button>
        )}
      </div>
    </div>
  </div>
)

// Matching game card from original
const renderMatchingGameCard = (game: any, onJoinGame: (game: any) => void, formatDateForDisplay: (date: string) => string) => {
  const hasClub = game.club && game.club.name;
  
  return (
    <div
      key={game.id}
      className="flex flex-col p-3 border rounded-lg bg-card hover:bg-accent/50 cursor-pointer transition-colors"
      onClick={() => onJoinGame(game)}
    >
      <div className="font-medium">{hasClub ? game.club.name : 'Partie organisée'}</div>

      <div className="flex justify-between items-center mt-2">
        <div className="flex flex-col">
          <div className="flex items-center text-xs text-muted-foreground">
            <Clock className="h-3 w-3 mr-1" />
            {game.time} - {game.endTime}
          </div>
          <div className="flex items-center text-xs text-muted-foreground mt-1">
            <Calendar className="h-3 w-3 mr-1" />
            {formatDateForDisplay(game.date)}
          </div>
          <div className="text-xs text-muted-foreground mt-1 font-medium">
            {game.spotsLeft > 1 ? `+${game.spotsLeft} places restantes` : "+1 place restante"}
          </div>
        </div>

        <div className="flex flex-col items-end gap-1">
          <Badge variant="outline" className="bg-success/40 text-success-foreground font-medium text-xs px-2 border-success/50">
            Niveau {game.level}
          </Badge>
          <div className="font-medium text-sm">{game.price}€</div>
          <Button
            size="sm"
            className="rounded-lg mt-1"
            onClick={(e) => {
              e.stopPropagation()
              onJoinGame(game)
            }}
          >
            Rejoindre
          </Button>
        </div>
      </div>
    </div>
  );
};

interface CreateGameResultsProps {
  isLoading: boolean;
  matchingGames: any[];
  alternativeGames: any[];
  onModifyPreferences: () => void;
  onCreateNewGame: () => void;
  onJoinGame: (game: any) => void;
  selectedDate: string;
  selectedTimeSlots: string[];
  selectedExactTime: string;
  showExactTime: boolean;
  selectedLevel: string;
  timeSlotOptions: any[];
}

export const CreateGameResults: React.FC<CreateGameResultsProps> = ({ 
  isLoading, 
  matchingGames, 
  alternativeGames, 
  onModifyPreferences, 
  onCreateNewGame, 
  onJoinGame, 
  selectedDate, 
  selectedTimeSlots, 
  selectedExactTime, 
  showExactTime, 
  selectedLevel, 
  timeSlotOptions 
}) => {
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long' });
  };

  const formatDateForDisplay = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' });
  };

  return (
    <>
      <CardHeader className="border-b border-border/50 py-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg">Résultats</CardTitle>
          <Button variant="ghost" size="sm" onClick={onModifyPreferences}>
            Modifier
          </Button>
        </div>
        <div className="flex flex-wrap gap-2 mt-2">
          <Badge variant="outline" className="bg-muted/50">
            <Calendar className="mr-1 h-3.5 w-3.5" />
            {formatDate(selectedDate)}
          </Badge>
          <Badge variant="outline" className="bg-muted/50">
            <Clock className="mr-1 h-3.5 w-3.5" />
            {showExactTime
              ? selectedExactTime
              : selectedTimeSlots.length > 0
                ? selectedTimeSlots
                    .map((id) => timeSlotOptions.find((opt) => opt.id === id)?.label.split(" ")[0])
                    .join(", ")
                : "Toute la journée"}
          </Badge>
          <Badge variant="outline" className="bg-success/40 text-success-foreground border-success/50">
            Niveau {selectedLevel}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        {isLoading ? (
          <div className="flex items-center justify-center p-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          </div>
        ) : matchingGames.length === 0 ? (
          <div className="p-6">
            <InfoMessage
              icon={<Calendar className="h-5 w-5 text-primary" />}
              title="Aucune partie trouvée"
              message={`Nous n'avons trouvé aucune partie correspondant à vos critères pour le ${formatDate(selectedDate)}.`}
              buttonText="Modifier mes critères"
              onButtonClick={onModifyPreferences}
            />

            {alternativeGames.length > 0 && (
              <div>
                <div className="flex items-center mb-5">
                  <div className="h-px flex-1 bg-border/50"></div>
                  <span className="px-3 text-[10px] uppercase tracking-wide text-muted-foreground/50 font-normal">Parties alternatives</span>
                  <div className="h-px flex-1 bg-border/50"></div>
                </div>

                <div className="mb-4">
                  <h3 className="text-lg font-medium mb-2">Parties recommandées pour vous</h3>
                  <p className="text-muted-foreground text-sm mb-5">
                    Voici des parties à d'autres moments qui pourraient vous intéresser.
                  </p>
                  <div className="space-y-3">
                    {alternativeGames.map((game) => {
                      // Map alternative games data to GameMatchDto format
                      const gameMatchDto = {
                        id: game.id,
                        dateTime: `${game.date}T${game.time}:00.000Z`,
                        location: game.club?.name || 'Partie organisée',
                        maxPlayers: 4,
                        currentPlayers: 4 - game.spotsLeft,
                        skillLevel: game.level,
                        hostDisplayName: '',
                        distanceKm: game.club?.distance || null,
                      }

                      return (
                        <GameMatchCard
                          key={game.id}
                          game={gameMatchDto}
                          isPerfectMatch={false}
                          onJoin={() => onJoinGame(game)}
                          searchCriteria={{
                            when: `${selectedDate}T${selectedExactTime || '00:00'}:00.000Z`,
                            where: game.club?.name || '',
                            level: parseInt(selectedLevel) || undefined
                          }}
                          isSecondaryAction={true}
                        />
                      )
                    })}
                  </div>
                </div>
              </div>
            )}
          </div>
        ) : (
          <div className="p-6 space-y-8">
            <div>
              <h3 className="text-lg font-medium mb-4">Parties trouvées pour vos critères</h3>
              <div className="space-y-3">{matchingGames.map((game) => renderMatchingGameCard(game, onJoinGame, formatDateForDisplay))}</div>
            </div>

            <div className="border-t border-border/50 pt-6">
              <div className="text-center">
                <p className="text-muted-foreground mb-4">Vous ne trouvez pas votre bonheur ?</p>
                <Button onClick={onCreateNewGame} className="w-full sm:w-auto">
                  <Plus className="mr-2 h-4 w-4" />
                  Créer une nouvelle partie
                </Button>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </>
  );
};
