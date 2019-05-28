﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CardGame.Cards;
using CardGame.UI;

namespace CardGame.Scenes {
    public class DeckScene : ChestScene {

        protected static new string title = "Deck";

        public DeckScene(bool spellMode) : base(spellMode) { }

        protected override void UpdateCards() {
            ClearChoices();
            Chest.Clear();
            Deck.Clear();

            foreach (Card c in Program.ActivePlayer.Chest) {
                if ((SpellMode && c is Spell) || (!SpellMode && c is Monster)) {
                    if (Chest.ContainsKey(c.ID)) Chest[c.ID]++;
                    else Chest.Add(c.ID,1);
                }
            }

            foreach (Card c in Program.ActivePlayer.Deck) {
                if ((SpellMode && c is Spell) || (!SpellMode && c is Monster)) {
                    if (Deck.ContainsKey(c.ID)) Deck[c.ID]++;
                    else Deck.Add(c.ID, 1);
                }
            }

            if (Deck.Count == 0) {
                SetTexts(new[] { "There are no cards in your deck. Head to your chest to fill it up!" });
                return;
            }

            foreach (int id in Deck.Keys) {
                Card c;
                if (SpellMode) c = new Spell(id);
                else c = new Monster(id);
                Dictionary<string, Action> choices = new Dictionary<string, Action>();
                choices.Add("Add to Deck", delegate() { AddToDeck(c); UpdateCards(); });
                choices.Add("Add to Chest", delegate() { AddToChest(c); UpdateCards(); });
                choices.Add("Back", delegate() { Program.Scene.EndSubscene(); });

                AddChoice(
                    "(" + id.ToString("000") + ") " + c.Name + " [" + Deck[id] + "-" + (Chest.ContainsKey(id) ? Chest[id] : 0) + "]",
                    delegate() {
                        Program.Scene.AddSubscene(
                            new TextScene(new CardSprite(c).Render(),"What do you want to do?",choices)
                        );
                    }
                );
            }

            AddChoice("Back", delegate() { Program.Scene.EndSubscene(); });
        }
    }
}