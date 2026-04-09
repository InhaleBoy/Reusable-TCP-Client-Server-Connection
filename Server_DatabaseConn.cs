using System;
using System.Globalization;
using Godot;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace traiding.script{
    public class DBConnection{

        public string Server { get; set; } = "localhost";
        public string UserName { get; set; } = "root";
        public string Password { get; set; } = "";
        public string DatabaseName { get; set; } = "trading game db";

        public MySqlConnection Connection { get; set; }

        public static NumberFormatInfo Nfi = new();
        private static DBConnection _instance;
        public static DBConnection Instance(){
            if (_instance is null) {
                _instance = new DBConnection();
                Nfi.NumberDecimalSeparator = ".";
            }
            return _instance;
        }

        public void Close(){
            Connection.Close();
            Connection = null;
        }

        public bool IsConnected(){
            if (Connection != null) return true;
            if (string.IsNullOrEmpty(DatabaseName)) return false;

            string connstring = $"Server={Server}; database={DatabaseName}; UID={UserName}; password={Password}";
            Connection = new MySqlConnection(connstring);
            
            try{
                Connection.Open();
            }
            catch (Exception){
                Connection = null;
                return false;
            }

            return true;
        }

        private MySqlDataReader Querry(string sql){
            var cmd = new MySqlCommand(sql, Connection);
            var reader = cmd.ExecuteReader();
            return reader;
        }

        // COMMANDS - the things that are accualy used in other codes
        // I prefere to o be heuse close inside those commands 
        // why bother about it anywhere else

        public bool AuthCheck(string login, string password){
            var reader = Querry($"SELECT COUNT(id_player) FROM players WHERE name = '{login}' AND password = '{password}'");
            reader.Read();
            var val = reader.GetInt32(0) == 1;
            Close();
            return val;
        }

        public Packet AuthResponsePacket(string login, string password){
            var reader = Querry($"SELECT id_player, name, money, shares FROM players WHERE name = '{login}' AND password = '{password}'");
            reader.Read();
            var packet = new Packet {
                Option = PacketOptions.AUTH_ASV_POSITIVE,
                ID = reader.GetInt64(0),
                PlayerInformation = new PlayerInformation {
                    Name = reader.GetString(1),
                    Money = reader.GetFloat(2),
                    OwnedShares = reader.GetFloat(3)
                }
            };
            Close();
            return packet;
        }

        public PlayerInformation GetPlayerInformation(long id){
            if (!IsConnected()) return null ;
            var reader = Querry($"SELECT name, money, shares FROM players WHERE id_player = {id}");
            reader.Read();
            PlayerInformation playerInformation = new PlayerInformation {
                Name = reader.GetString(0),
                Money = reader.GetFloat(1),
                OwnedShares = reader.GetFloat(2)
            };
            Close();
            return playerInformation;
        }

        public void TransactionHandler(long id, float money, float shares){
            if (!IsConnected()) return;
            string sql = $"UPDATE players SET money = money + {money.ToString(Nfi)}, shares = shares + {shares.ToString(Nfi)} WHERE id_player = {id}";
            Querry(sql);
            Close();
        }

        public Dictionary<string,float> GetLadder(){
            if (!IsConnected()) return null;
            var reader = Querry("SELECT name, money FROM players ORDER BY money DESC");
            Dictionary<string,float> dict = new();       
            while(reader.Read()){
                dict.Add(reader.GetString(0),reader.GetFloat(1));
            }
            Close();
            return dict;
        }
    }
}