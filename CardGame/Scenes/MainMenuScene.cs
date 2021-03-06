﻿using System.Text;
using CardGame.Characters;
using ConsoleUI;

namespace CardGame.Scenes {
    public class MainMenuScene : TextScene {
        public MainMenuScene() : base("Choose your option.") {
            AddChoice("Campaign", StartCampaign);
            AddChoice("Deck", OpenDeck);
            AddChoice("Chest", OpenChest);
            AddChoice("Player", ShowPlayerInfo);
            AddChoice("Quit", QuitToMenu);
        }
        
        public void StartCampaign() {
            AddSubscene(new CampaignScene());
        }

        public void OpenDeck() {
            AddSubscene(new DeckScene(false));
        }

        public void OpenChest() {
            AddSubscene(new ChestScene(false));
        }

        public void ShowPlayerInfo() {
            Player p = Program.ActivePlayer;
            StringBuilder pText = new StringBuilder("Name: " + p.Name +
                "\nLevel: " + p.Level +
                "\nWins: " + p.Wins +
                "\nLosses: " + p.Losses +
                "\nTies: " + p.Ties +
                "\nGames Played: " + (p.Wins + p.Losses + p.Ties) +
                "\nWin/Loss Ratio: " + string.Format("{0:0.00}", p.Losses != 0 ? p.Wins / (double) p.Losses : 0D)
            );
            pText.AppendLine();
            foreach (NPC n in NPC.GetAllNPCs()) pText.AppendLine(
                n.Name + ": Wins: " + p.GetWins(n.ID)
                + " Losses: " + p.GetLosses(n.ID) 
                + " Ties: " + p.GetTies(n.ID) 
                + " Total: " + p.GetTotalGames(n.ID)
            );

            AddSubscene(new TextScene("Player Stats", pText.ToString()));
        }

        public void QuitToMenu() {
            TextScene confirm = new TextScene("Warning", "Are you sure you want to quit?");
            confirm.AddChoice("Yes", delegate() { Program.SceneManager.SetBaseScene(new TitleScene()); });
            confirm.AddChoice("No", delegate() { confirm.EndScene(); });
            AddSubscene(confirm);
        }
    }
}
