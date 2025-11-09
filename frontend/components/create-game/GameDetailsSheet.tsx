"use client";

import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, MapPin, Clock, Users, Plus, Check, Calendar } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

interface GameDetailsSheetProps {
  game: any;
  isOpen: boolean;
  onClose: () => void;
  onJoin: () => void;
  onAddToCalendar: () => void;
  onInviteFriend: () => void;
  onOpenChat: () => void;
  showJoinConfirmation: boolean;
}

export const GameDetailsSheet: React.FC<GameDetailsSheetProps> = ({ 
  game, 
  isOpen, 
  onClose, 
  onJoin,
  onAddToCalendar,
  onInviteFriend,
  onOpenChat,
  showJoinConfirmation
}) => {
  if (!game) return null;

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/60 z-40"
            onClick={onClose}
          />
          <motion.div
            initial={{ y: '100%' }}
            animate={{ y: 0 }}
            exit={{ y: '100%' }}
            transition={{ type: 'spring', damping: 30, stiffness: 300 }}
            className="fixed bottom-0 left-0 right-0 h-[90vh] bg-background z-50 rounded-t-2xl flex flex-col"
          >
            <div className="p-4 border-b border-border/50 flex-shrink-0">
              <div className="flex justify-between items-center">
                <h3 className="font-bold text-lg">Détails de la partie</h3>
                <Button variant="ghost" size="icon" onClick={onClose} className="rounded-full">
                  <X className="h-5 w-5" />
                </Button>
              </div>
            </div>
            <div className="flex-grow overflow-y-auto p-6 space-y-6">
              <div className="flex items-center gap-4">
                <Avatar className="h-16 w-16">
                  <AvatarImage src={game.club?.logo} />
                  <AvatarFallback>{game.club?.name?.charAt(0) || 'P'}</AvatarFallback>
                </Avatar>
                <div>
                  <h2 className="font-bold text-2xl">{game.club?.name || 'Partie Organisée'}</h2>
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <MapPin className="h-4 w-4" /> <span>{game.club?.address || game.location}</span>
                  </div>
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Clock className="h-4 w-4" /> <span>{game.time}</span>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-3 gap-4 text-center">
                <div>
                  <p className="font-bold text-lg">{game.level}</p>
                  <p className="text-sm text-muted-foreground">Niveau</p>
                </div>
                <div>
                  <p className="font-bold text-lg">{4 - game.spotsLeft}/{4}</p>
                  <p className="text-sm text-muted-foreground">Joueurs</p>
                </div>
                <div>
                  <p className="font-bold text-lg">{game.club?.distance ? `${game.club.distance} km` : 'N/A'}</p>
                  <p className="text-sm text-muted-foreground">de vous</p>
                </div>
              </div>

              <div className="space-y-3">
                <h4 className="font-semibold">Joueurs inscrits</h4>
                {game.players.map((player: any, i: number) => (
                  <div key={i} className="flex items-center gap-3 p-3 bg-muted/50 rounded-lg">
                    <Avatar className="h-8 w-8"><AvatarImage src={player.avatar} /><AvatarFallback>{player.name.charAt(0)}</AvatarFallback></Avatar>
                    <div>
                      <div className="font-medium text-sm">{player.name}</div>
                      <div className="text-xs text-muted-foreground">Niveau {player.level}</div>
                    </div>
                  </div>
                ))}
                {Array.from({ length: game.spotsLeft }).map((_, i) => (
                  <div key={`empty-${i}`} className="flex items-center gap-3 p-3 border-2 border-dashed border-border/50 rounded-lg">
                    <div className="w-8 h-8 rounded-full border-2 border-dashed border-border/50 flex items-center justify-center"><Plus className="h-4 w-4 text-muted-foreground" /></div>
                    <div className="text-sm text-muted-foreground">Place disponible</div>
                  </div>
                ))}
              </div>
            </div>
            <div className="sticky bottom-0 bg-background/95 backdrop-blur-sm border-t border-border/50 p-6 flex-shrink-0">
              <Button onClick={onJoin} className="w-full py-3 text-base font-semibold rounded-xl" size="lg">
                {game.hasBookedClub ? 'Rejoindre cette partie' : 'Rejoindre et aider à organiser'}
              </Button>
              {!game.hasBookedClub && (
                <div className="text-xs text-center text-muted-foreground mt-2">Vous participerez au choix du club avec les autres joueurs</div>
              )}
            </div>
          </motion.div>
        </>
      )}

      {showJoinConfirmation && (
        <motion.div initial={{ y: 100, opacity: 0 }} animate={{ y: 0, opacity: 1 }} exit={{ y: 100, opacity: 0 }} className="fixed bottom-6 left-4 right-4 z-50 mx-auto max-w-md">
          <div className="bg-success text-success-foreground rounded-xl p-4 shadow-lg">
            <div className="flex items-center gap-3 mb-3">
              <div className="w-8 h-8 rounded-full bg-success-foreground/20 flex items-center justify-center"><Check className="h-5 w-5" /></div>
              <div>
                <div className="font-semibold">C'est confirmé !</div>
                <div className="text-sm opacity-90">Vous avez rejoint la partie</div>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <Button variant="secondary" size="sm" onClick={onAddToCalendar} className="bg-success-foreground/20 text-success-foreground hover:bg-success-foreground/30 border-0"><Calendar className="mr-1 h-4 w-4" />Calendrier</Button>
              {game?.spotsLeft > 1 && (
                <Button variant="secondary" size="sm" onClick={onInviteFriend} className="bg-success-foreground/20 text-success-foreground hover:bg-success-foreground/30 border-0"><Plus className="mr-1 h-4 w-4" />Inviter</Button>
              )}
              <Button variant="secondary" size="sm" onClick={onOpenChat} className="bg-success-foreground/20 text-success-foreground hover:bg-success-foreground/30 border-0 col-span-2">Ouvrir le chat</Button>
            </div>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
};