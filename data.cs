/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MovingAverageExpert
{
    class data
    {
            private MySqlConnection connection;
            private String server;
            private String database;
            private String uid;
            private String password;
            private String id, nom, prenom, email;  // A VOIR JE SAIS PAS


            public data()
            {
                Initializer();
            }

            public void Initializer()
            {
                server = "sql151.main-hosting.eu";
                database = "u918281900_tbot";
                uid = "u918281900_zrif";
                password = "69NtTBnzrghW";

                String connectionString;
                connectionString = "SERVER=" + server + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
                connection = new MySqlConnection(connectionString);
            }

            public bool OpenConnection()
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Connection Impossible au serveur");
                    switch (ex.Number)
                    {
                        case 0:
                            Console.WriteLine("Connection Impossible au serveur");
                            break;
                        case 1045:
                            Console.WriteLine("Mot de passe / ID incorrect");
                            break;
                    }
                    return false;
                }
            }

            public bool CloseConnection()
            {
                try
                {
                    connection.Close();
                    return true;
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }


            public void InsertTrade(int numberBacktest)
            {
                String query = "INSERT INTO `BotTrade`(`number_backtest`, `number_trade`, `time_frame`, `entryTrade`, `exitTrade`, `asset`, `direction`, `entry_price`, `exit_price`, `take_profit`, `stop_loss`, `volume`, `profit`, `loss`, `commission`, `commentTrade`) VALUES (@numberBacktest,@numberTrade,@time_frame,@entryDate,@exitDate,@asset,@direction,@entryPrice,@exitPrice,@takeProfit,@stopLoss,@volume,@profit,@loss,@commission,@comment) ";
                MySqlCommand command = new MySqlCommand(query, connection);

                command.Parameters.AddWithValue("@numberBacktest", numberBacktest);
                command.Parameters.AddWithValue("@numberTrade", numberBacktest);
                command.Parameters.AddWithValue("@time_frame", numberBacktest);
                command.Parameters.AddWithValue("@entryDate", numberBacktest);
                command.Parameters.AddWithValue("@exitDate", numberBacktest);
                command.Parameters.AddWithValue("@asset", numberBacktest);
                command.Parameters.AddWithValue("@direction", numberBacktest);
                command.Parameters.AddWithValue("@entryPrice", numberBacktest);
                command.Parameters.AddWithValue("@exitPrice", numberBacktest);
                command.Parameters.AddWithValue("@takeProfit", numberBacktest);
                command.Parameters.AddWithValue("@stopLoss", numberBacktest);
                command.Parameters.AddWithValue("@volume", numberBacktest);
                command.Parameters.AddWithValue("@profit", numberBacktest);
                command.Parameters.AddWithValue("@loss", numberBacktest);
                command.Parameters.AddWithValue("@commission", numberBacktest);
                command.Parameters.AddWithValue("@comment", numberBacktest);


                connection.Open();
                int result = command.ExecuteNonQuery();
                if (result < 0) Console.WriteLine("Error in data base");
            }


    }
}
*/