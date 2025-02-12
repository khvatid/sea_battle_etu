﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace SeaBattleConsole
{
    internal class Board
    {
        private int playerHP, enemyHP, shipsPlaced;
        private bool turn, planning, placement, afplaning, host, connected, lobby, getHostIP;
        private static TcpClient client;
        private Cell[,] playerField = new Cell[10, 10];
        private Cell[,] enemyField = new Cell[10, 10];

        public Board()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    playerField[i, j] = new Cell(false);
                    enemyField[i, j] = new Cell(true);
                }
            }
            client = new TcpClient();
            shipsPlaced = 0;

            playerHP = 20;
            enemyHP = playerHP;

            placement = false;
            afplaning = true;
            connected = false;
            lobby = true;
            planning = true;
            getHostIP = true;
            turn = false;
        }

        public static void PlaySound(string file)
        {
            string path = Path.Combine(Environment.CurrentDirectory, @"sounds\");

            string pathSTR = path + file;

            Process.Start(@"powershell", $@"-c (New-Object Media.SoundPlayer '{pathSTR}').PlaySync();");
        }

        public static void stopSound()
        {
            //Process.Start("cmd.exe", "/c taskkill /F /IM powershell.exe");

            Process[] proc = Process.GetProcesses();
            foreach (Process process in proc)
                if (process.ProcessName == "powershell")
                {
                    process.Kill();
                }
        }

        public void start()
        {
            //client = new TcpClient();

            try
            {
                client.Connect("93.191.58.52", 420);//93.191.58.52
                if (client.Connected)
                {
                    NetworkStream serverCennel = client.GetStream();
                    int i = 0;
                    Byte[] b = new Byte[255];
                    while ((i = serverCennel.Read(b, 0, b.Length)) != 0)
                    {
                        string sAnswer = System.Text.Encoding.Unicode.GetString(b, 0, i);
                        Console.WriteLine(sAnswer);
                        stopSound();
                        PlaySound("place.wav");
                        if (sAnswer == "player found")
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            Console.Clear();
            while (true)
            {
                drawUI(); // gameplay loop
                if (enemyHP == 0 || playerHP == 0) return;
            }
        }

        private void plan()
        {
            if (shipsPlaced >= 10)
            {
                afplaning = true;
                planning = false;
                return;
            }

            Console.Write("Enter position for ");

            int length = 0;

            if (shipsPlaced == 0)
                length = 4;
            if (shipsPlaced > 0 && shipsPlaced < 3)
                length = 3;
            if (shipsPlaced > 2 && shipsPlaced < 6)
                length = 2;
            if (shipsPlaced > 5)
                length = 1;
            Console.Write(length);

            Console.Write("-cell ship (f. e. B4 or d0).\n");
            Console.Write("Current placement mode: ");
            if (placement)
                Console.Write("||");
            else
                Console.Write("═");
            Console.Write(" . Enter R to change mode.\n");

            string input = Console.ReadLine();
            int cache = validCheck(input);
            if (cache == -1)
            {
                PlaySound("rotate.wav");
                return;
            }

            PlaySound("place.wav");
            placeShip(getRow(cache), getColumn(cache), length);
        }

        private void afterPlan()
        {
            Console.Write("All ships dispatched, waiting for enemy...");
            NetworkStream stream = client.GetStream();
            Byte[] b = new Byte[20];
            Byte[] bSend = System.Text.Encoding.GetEncoding(866).GetBytes("2588");
            stream.Write(bSend);
            int i = 0;
            while ((i = stream.Read(b, 0, b.Length)) != 0)
            {
                string messege = System.Text.Encoding.Unicode.GetString(b, 0, i);
                if (messege == "1")
                {
                    turn = true;
                    return;
                }
                if (messege == "0")
                {
                    turn = false;
                    return;
                }
                if (messege == "enemy done")
                {
                    PlaySound("start.wav");
                    Console.WriteLine(messege);
                    afplaning = false;
                }
            }
        }

        private void drawUI()
        {
            Console.Clear(); // refresh console, clear old frame

            drawLogo();

            //connectionSetup();

            Console.WriteLine($"Player HP: {playerHP} \t\tEnemy HP: {enemyHP}\n"); // HPs information

            Console.WriteLine("  ABCDEFGHIJ\t\t  ABCDEFGHIJ");

            for (int i = 0; i < 10; i++) // rows
            {
                Console.Write(i);
                Console.Write(" ");

                for (int j = 0; j < 10; j++) // player columns
                    drawCell(playerField[i, j]);

                Console.Write("\t\t");

                Console.Write(i);
                Console.Write(" ");

                for (int j = 0; j < 10; j++) // enemy columns
                    drawCell(enemyField[i, j]);

                Console.Write("\n"); // end of the line (row)
            }

            Console.Write("\n");

            if (planning)
            {
                plan();
                return;
            }

            if (afplaning)
                afterPlan();

            if (playerHP == 0)
            {
                PlaySound("lose.wav");
                Console.WriteLine(@"

 @@@@@                                        @@@@@
@@@@@@@                                      @@@@@@@
@@@@@@@           @@@@@@@@@@@@@@@            @@@@@@@
 @@@@@@@@       @@@@@@@@@@@@@@@@@@@        @@@@@@@@
     @@@@@     @@@@@@@@@@@@@@@@@@@@@     @@@@@
       @@@@@  @@@@@@@@@@@@@@@@@@@@@@@  @@@@@
         @@  @@@@@@@@@@@@@@@@@@@@@@@@@  @@
            @@@@@@@    @@@@@@    @@@@@@
            @@@@@@      @@@@      @@@@@
            @@@@@@      @@@@      @@@@@
             @@@@@@    @@@@@@    @@@@@
              @@@@@@@@@@@  @@@@@@@@@@
               @@@@@@@@@@  @@@@@@@@@
           @@   @@@@@@@@@@@@@@@@@   @@
           @@@@  @@@@ @ @ @ @ @@@@  @@@@
          @@@@@   @@@ @ @ @ @ @@@   @@@@@
        @@@@@      @@@@@@@@@@@@@      @@@@@
      @@@@          @@@@@@@@@@@          @@@@
   @@@@@              @@@@@@@              @@@@@
  @@@@@@@                                 @@@@@@@
   @@@@@                                   @@@@@

             _      ____   _____ ______
            | |    / __ \ / ____|  ____|
            | |   | |  | | (___ | |__
            | |   | |  | |\___ \|  __|
            | |___| |__| |____) | |____
            |______\____/|_____/|______|

You've lost... Game will be closed in 5 seconds.");
                System.Threading.Thread.Sleep(5000);
                client.Close();
                return;
            }
            if (enemyHP == 0)
            {
                PlaySound("win.wav");
                Console.WriteLine(@"

         .* *.               `o`o`
         *. .*              o`o`o`o      ^,^,^
           * \               `o`o`     ^,^,^,^,^
              \     ***        |       ^,^,^,^,^
               \   *****       |        /^,^,^
                \   ***        |       /
    ~@~*~@~      \   \         |      /
  ~*~@~*~@~*~     \   \        |     /
  ~*~@smd@~*~      \   \       |    /     #$#$#        .`'.;.
  ~*~@~*~@~*~       \   \      |   /     #$#$#$#   00  .`,.',
    ~@~*~@~ \        \   \     |  /      /#$#$#   /|||  `.,'
_____________\________\___\____|_/______/_________|\/\___||______

               __          _______ _   _
               \ \        / /_   _| \ | |
                \ \  /\  / /  | | |  \| |
                 \ \/  \/ /   | | | . ` |
                  \  /\  /   _| |_| |\  |
                   \/  \/   |_____|_| \_|

You've won! Game will be closed in 5 seconds.");
                System.Threading.Thread.Sleep(5000);
                client.Close();
                return;
            }

            if (turn)
                makeTurn();
            else
                waitTurn();
        }

        public void drawLogo()
        {
            string text = @"
   _____ ______            ____       _______ _______ _      ______
  / ____|  ____|   /\     |  _ \   /\|__   __|__   __| |    |  ____|
 | (___ | |__     /  \    | |_) | /  \  | |     | |  | |    | |__
  \___ \|  __|   / /\ \   |  _ < / /\ \ | |     | |  | |    |  __|
  ____) | |____ / ____ \  | |_) / ____ \| |     | |  | |____| |____
 |_____/|______/_/    \_\ |____/_/    \_\_|     |_|  |______|______|";

            Console.Write(text);
            Console.Write("\n\n");
        }

        private void drawCell(Cell cell)
        {
            int status = cell.GetStatus();
            var recievedBytes = new byte[256];

            for (int i = 0; i < recievedBytes.Length; i++)
            {
                recievedBytes[i] = (byte)i;
            }
            var str = Encoding.GetEncoding(866).GetString(recievedBytes);
            

            switch (status)
            {
                case 0:
                    Console.Write(str[178]); // fog
                    break;

                case 1:
                    Console.Write(str[176]); // empty unbombed
                    break;

                case 2:
                    Console.Write("*"); // empty bombed
                    break;

                case 3:
                    Console.Write("O"); // ship unbombed
                    break;

                case 4:
                    Console.Write("X"); // ship bombed
                    break;
            }
        }

        private void makeTurn()
        {
            Console.WriteLine("Enter coordinates to bomb (if you damage enemy ship he skips the turn): ");
            string input = Console.ReadLine();
            int cache = validCheck(input);
            if (cache == -1)
                return;
            else
            {
                int row = getRow(cache);
                int column = getColumn(cache);
                enemyField[row, column].Bomb();
                NetworkStream stream = client.GetStream();
                Byte[] b = new Byte[5] { (byte)cache, 0, 0, 0, 0 };
                stream.Write(b);
                int i = 0;
                while ((i = stream.Read(b, 0, b.Length)) != 0)
                {
                    int result = BitConverter.ToInt32(b);
                    switch (result)
                    {
                        case 0:
                            PlaySound("miss.wav");
                            turn = false;
                            return;

                        case 1:
                            PlaySound("explosion.wav");
                            enemyField[row, column].PlaceShip();
                            enemyHP--;
                            return;

                        case 2:
                            PlaySound("explosion.wav");
                            enemyField[row, column].PlaceShip();
                            enemyHP--;
                            destructionReveal(cache);
                            return;

                        default: break;
                    }
                }
            }
        }

        private void waitTurn()
        {
            Console.WriteLine("BOMBS!!! Brace!");
            NetworkStream stream = client.GetStream();
            Byte[] b = new Byte[5];
            int i = 0;
            while ((i = stream.Read(b, 0, b.Length)) != 0)
            {
                int rowColumn = BitConverter.ToInt32(b, 0);
                Byte[] bSend = new Byte[1] { (byte)getBombed(rowColumn) };
                stream.Write(bSend);
                break;
            }
        }

        private int validCheck(string input)
        {
            int length = input.Length;

            switch (length)
            {
                case 1:
                    if (planning)
                        if (input == "R" || input == "r")
                            placement = !placement;
                    return -1;

                case 2:
                    {
                        int column = input[0];
                        int row = input[1];

                        bool columnValid = false;
                        bool rowValid = false;

                        if (column > 64 && column < 75)
                        {
                            columnValid = true;
                            column -= 65;
                        }
                        if (column > 96 && column < 107)
                        {
                            columnValid = true;
                            column -= 97;
                        }
                        if (row > 47 && row < 58)
                        {
                            rowValid = true;
                            row -= 48;
                        }

                        if (columnValid && rowValid)
                            return row * 10 + column;
                        else
                            return -1;
                    }
                default:
                    return -1;
            }
        }

        private bool placeShip(int row, int column, int length)
        {
            int lengthleft = length + 1;

            if (placement) // vertical
            {
                for (int j = column - 1; j < column + 2; j++)
                {
                    if (j <= -1 || j >= 10) // out of border, no need to check there
                        continue;

                    for (int i = row - 1; i < row + length + 1; i++)
                    {
                        if (i <= -1 || i >= 10) // out of border, no need to check there
                            continue;

                        if (playerField[i, j].GetShip())
                            return false;

                        lengthleft--;
                    }

                    if (lengthleft > 0) // ship can't fit here because of borders
                        return false;
                }
            }
            else // horizontal
            {
                for (int i = row - 1; i < row + 2; i++)
                {
                    if (i <= -1 || i >= 10) // out of border, no need to check there
                        continue;

                    for (int j = column - 1; j < column + length + 1; j++)
                    {
                        if (j <= -1 || j >= 10) // out of border, no need to check there
                            continue;

                        if (playerField[i, j].GetShip())
                            return false;

                        lengthleft--;
                    }

                    if (lengthleft > 0) // ship can't fit here because of borders
                        return false;
                }
            }

            for (int i = 0; i < length; i++)
            {
                playerField[row, column].PlaceShip();

                if (placement) // vertical
                    row++;
                else
                    column++; // horizontal
            }

            shipsPlaced++;
            return true;
        }

        private bool shipDestroyed(int rowColumn)
        {
            int row = getRow(rowColumn);
            int column = getColumn(rowColumn);

            int rowUp = row - 1;
            int rowDown = row + 1;
            int columnLeft = column - 1;
            int columnRight = column + 1;
            bool destroyedShip = true;
            if (destroyedShip)
                for (; rowUp > -1; rowUp--) // looking up for more undamaged parts of the same ship
                {
                    if (!playerField[rowUp, column].GetShip()) // check if there is even a ship
                        break;
                    if (playerField[rowUp, column].GetShip() && !playerField[rowUp, column].GetBombed())
                    {
                        destroyedShip = false;
                        break;
                    }
                }

            if (destroyedShip)
                for (; rowDown < 10; rowDown++) // looking down for more undamaged parts of the same ship
                {
                    if (!playerField[rowDown, column].GetShip()) // check if there is even a ship
                        break;
                    if (playerField[rowDown, column].GetShip() && !playerField[rowDown, column].GetBombed())
                    {
                        destroyedShip = false;
                        break;
                    }
                }

            if (destroyedShip)
                for (; columnLeft > -1; columnLeft--) // looking left for more undamaged parts of the same ship
                {
                    if (!playerField[row, columnLeft].GetShip()) // check if there is even a ship
                        break;
                    if (playerField[row, columnLeft].GetShip() && !playerField[row, columnLeft].GetBombed())
                    {
                        destroyedShip = false;
                        break;
                    }
                }

            if (destroyedShip)
                for (; columnRight < 10; columnRight++) // looking right for more undamaged parts of the same ship
                {
                    if (!playerField[row, columnRight].GetShip()) // check if there is even a ship
                        break;
                    if (playerField[row, columnRight].GetShip() && !playerField[row, columnRight].GetBombed())
                    {
                        destroyedShip = false;
                        break;
                    }
                }

            if (destroyedShip)
                return true;

            return false;
        }

        private void bubbleReveal(int rowColumn)
        {
            int row = getRow(rowColumn);
            int column = getColumn(rowColumn);

            for (int i = row - 1; i < row + 2; i++) // this loop will proc 4, 6 or 9 times, depends on border proximity
            {
                if (i <= -1 || i >= 10) // out of border
                    continue;

                for (int j = column - 1; j < column + 2; j++)
                {
                    if (j <= -1 || j >= 10) // out of border
                        continue;

                    enemyField[i, j].Reveal();
                }
            }
        }

        private void destructionReveal(int rowColumn)
        {
            bubbleReveal(rowColumn);

            int row = getRow(rowColumn);
            int column = getColumn(rowColumn);

            int rowUp = row - 1;
            int rowDown = row + 1;
            int columnLeft = column - 1;
            int columnRight = column + 1;
            for (; rowUp > -1; rowUp--) // looking up for more parts of this ship
            {
                if (!enemyField[rowUp, column].GetShip()) // check if there is even a ship
                    break;
                bubbleReveal(rowUp * 10 + column);
            }

            for (; rowDown < 10; rowDown++) // looking down for more parts of this ship
            {
                if (!enemyField[rowDown, column].GetShip()) // check if there is even a ship
                    break;
                bubbleReveal(rowDown * 10 + column);
            }

            for (; columnLeft > -1; columnLeft--) // looking left for more parts of this ship
            {
                if (!enemyField[row, columnLeft].GetShip()) // check if there is even a ship
                    break;
                bubbleReveal(row * 10 + columnLeft);
            }

            for (; columnRight < 10; columnRight++) // looking right for more parts of this ship
            {
                if (!enemyField[row, columnRight].GetShip()) // check if there is even a ship
                    break;
                bubbleReveal(row * 10 + columnRight);
            }
        }

        private int getRow(int rowColumn)
        {
            return rowColumn / 10;
        }

        private int getColumn(int rowColumn)
        {
            return rowColumn % 10;
        }

        private int getBombed(int rowColumn)
        {
            int row = getRow(rowColumn);
            int column = getColumn(rowColumn);
            turn = true;
            if (playerField[row, column].GetBombed())
            {               // you can't bomb the ship part 2 times
                PlaySound("miss.wav");
                return 0;
            }

            if (playerField[row, column].Bomb())
            {
                PlaySound("explosion.wav");
                playerHP--;
                turn = false;
                if (shipDestroyed(rowColumn))
                {
                    return 2; // send shipDestoyed signal
                }

                return 1;
            }
            PlaySound("miss.wav");
            return 0;
        }
    }
}

/*
 *  client = new TcpClient();
            client.Connect("93.191.58.52", 420);

            Task.Factory.StartNew(() =>
            {
                NetworkStream serverCennel = client.GetStream();
                int i = 0;
                Byte[] b = new Byte[255];
                while ((i = serverCennel.Read(b, 0, b.Length)) != 0)
                {
                    Console.WriteLine(System.Text.Encoding.Unicode.GetString(b, 0, i));
                }
            });

            NetworkStream stream = client.GetStream();
            while (true)
            {
                Byte[] data = System.Text.Encoding.Unicode.GetBytes(Console.ReadLine());
                stream.Write(data);
            }
 *
 */