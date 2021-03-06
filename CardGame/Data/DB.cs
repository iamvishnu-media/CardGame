﻿using CardGame.Cards;
using CardGame.Characters;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using Vergil.Data.DB;
using Vergil.Utilities;

namespace CardGame.Data {
    public class DB {
        private static DBConnection connection = null;

        private static void OpenDatabaseConnection() {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "cg.db");
            if (!File.Exists(path)) CreateDatabase();

            if (connection == null) connection = new SQLiteConnection(path);
            if (!connection.IsOpen) connection.Open();
        }

        private static void CloseDatabaseConnection() {
            if (connection == null) return;
            if (connection.IsOpen) connection.Close();
            connection.Dispose();
            connection = null;
        }

        public static void CreateDatabase() {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "cg.db");
            if (!File.Exists(path)) SQLiteConnection.CreateDatabase(path);
            OpenDatabaseConnection();

            List<DBFieldDefinition> fields;
            if (!connection.TableExists("monster")) {
                fields = new List<DBFieldDefinition>() {
                    new DBFieldDefinition("monster_id","INTEGER",true),
                    new DBFieldDefinition("name","TEXT"),
                    new DBFieldDefinition("description","TEXT"),
                    new DBFieldDefinition("attack","INTEGER"),
                    new DBFieldDefinition("defense","INTEGER"),
                    new DBFieldDefinition("level","INTEGER"),
                    new DBFieldDefinition("type","TEXT")
                };
                connection.AddTable("monster", fields);
            }

            if (!connection.TableExists("spell")) {
                fields = new List<DBFieldDefinition>() {
                    new DBFieldDefinition("spell_id","INTEGER",true),
                    new DBFieldDefinition("name","TEXT"),
                    new DBFieldDefinition("description","TEXT"),
                    new DBFieldDefinition("level","INTEGER"),
                    new DBFieldDefinition("type","TEXT"),
                    new DBFieldDefinition("trigger","TEXT")
                };

                connection.AddTable("spell", fields);
            }

            if (!connection.TableExists("effects")) {
                fields = new List<DBFieldDefinition>() {
                    new DBFieldDefinition("spell_id","INTEGER",true),
                    new DBFieldDefinition("effect_index","INTEGER",true),
                    new DBFieldDefinition("assignment","TEXT"),
                    new DBFieldDefinition("target","TEXT"),
                    new DBFieldDefinition("range","TEXT"),
                    new DBFieldDefinition("action","TEXT"),
                    new DBFieldDefinition("stat","TEXT"),
                    new DBFieldDefinition("amount","INTEGER")
                };

                connection.AddTable("effects", fields);
            }

            if (!connection.TableExists("player")) {
                fields = new List<DBFieldDefinition>() {
                    new DBFieldDefinition("player_id","INTEGER",true),
                    new DBFieldDefinition("name","TEXT"),
                    new DBFieldDefinition("level","INTEGER")
                };

                connection.AddTable("player", fields);
            }

            if (!connection.TableExists("battle_record")) {
                fields = new List<DBFieldDefinition>() {
                    new DBFieldDefinition("player_id","INTEGER",true),
                    new DBFieldDefinition("npc_id","INTEGER",true),
                    new DBFieldDefinition("wins","INTEGER"),
                    new DBFieldDefinition("losses","INTEGER"),
                    new DBFieldDefinition("ties","INTEGER")
                };

                connection.AddTable("battle_record", fields);
            }

            if (!connection.TableExists("card_collections")) {
                fields = new List<DBFieldDefinition>() {
                    new DBFieldDefinition("character_id","INTEGER",true),
                    new DBFieldDefinition("is_player","INTEGER",true),
                    new DBFieldDefinition("card_id","INTEGER", true),
                    new DBFieldDefinition("is_spell","INTEGER", true),
                    new DBFieldDefinition("deck_count","INTEGER"),
                    new DBFieldDefinition("chest_count","INTEGER")
                };

                connection.AddTable("card_collections", fields);
            }

            if (!connection.TableExists("npc")) {
                fields = new List<DBFieldDefinition>() {
                    new DBFieldDefinition("npc_id","INTEGER",true),
                    new DBFieldDefinition("name","TEXT"),
                    new DBFieldDefinition("description","TEXT"),
                    new DBFieldDefinition("bounty","INTEGER"),
                    new DBFieldDefinition("level","INTEGER"),
                    new DBFieldDefinition("difficulty","INTEGER")
                };

                connection.AddTable("npc", fields);
            }

            if (!connection.TableExists("npc_text")) {
                fields = new List<DBFieldDefinition>() {
                    new DBFieldDefinition("npc_id","INTEGER",true),
                    new DBFieldDefinition("text_id","INTEGER",true),
                    new DBFieldDefinition("text_content","TEXT")
                };

                connection.AddTable("npc_text", fields);
            }
            CloseDatabaseConnection();
        }

        public static void PopulateTables() {
            OpenDatabaseConnection();
            foreach (Card c in Card.GetAllCards()) SaveCard(c);
            foreach (Player p in Player.GetAllPlayers()) SavePlayer(p);
            CloseDatabaseConnection();
        }

        public static void SaveCard(Card card) {
            if (card is Monster) {
                Monster mon = (Monster)card;
                connection.AddRecord("monster", new[] { mon.ID.ToString(), mon.Name, mon.Description, mon.Attack.ToString(), mon.Defense.ToString(), mon.Level.ToString(), mon.Type.GetName() }, "monster_id=" + mon.ID);
            } else if (card is Spell) {
                Spell spl = (Spell)card;
                connection.AddRecord("spell", new[] { spl.ID.ToString(), spl.Name, spl.Description, spl.Level.ToString(), spl.EffectType.GetName(), spl.Trigger.GetName() }, "spell_id=" + spl.ID);
            }

            foreach (CardEffect e in card.Effects) {
                connection.AddRecord("effects", new[] { card.ID.ToString(), card.Effects.IndexOf(e).ToString(), e.TargetAssignment.GetName(), e.TargetType.GetName(), e.Range.GetName(), e.Action.GetName(), e.EffectStat.GetName(), e.Amount.ToString() }, "spell_id=" + card.ID);
            }
        }

        public static void SavePlayer(Player player) {
            connection.AddRecord("player", new[] { player.ID.ToString(), player.Name, player.Level.ToString() }, "player_id=" + player.ID);

            foreach (List<Card> list in new[] { player.Chest, player.Deck }) {
                foreach (Card c in list) {
                    using (DbDataReader reader = connection.Select("card_collections",new[] { "deck_count", "chest_count" }, string.Format("character_id={0} AND is_player=1 AND card_id={1} and is_spell={2}", player.ID.ToString(), c.ID.ToString(), c is Spell ? 1 : 0))) {
                        reader.Read();
                        if (!reader.HasRows) {
                            connection.Insert("card_collections", new[] { "character_id", "is_player", "card_id", "is_spell", "deck_count", "chest_count" }, new[] { player.ID.ToString(), "1", c.ID.ToString(), c is Spell ? "1" : "0", list == player.Deck ? "1" : "0", list == player.Deck ? "0" : "1" });
                            continue;
                        }

                        int count = reader.GetInt32(list == player.Deck ? 0 : 1) + 1;
                        connection.Update("card_collections", new[] { (list == player.Deck ? "deck" : "chest") + "_count" }, new[] { count.ToString() }, string.Format("character_id={0} AND is_player=1 AND card_id={1} and is_spell={2}", player.ID.ToString(), c.ID.ToString(), c is Spell ? 1 : 0));
                    }
                }
            }
            
            foreach (BattleRecord rec in player.Record) {
                connection.AddRecord("battle_record", new[] { player.ID.ToString(), rec.OpponentID.ToString(), rec.Wins.ToString(), rec.Losses.ToString(), rec.Ties.ToString() }, string.Format("player_id={0} AND npc_id={1}",player.ID, rec.OpponentID));
            }
        }
    }
}
