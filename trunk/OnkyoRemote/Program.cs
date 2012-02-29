﻿using System;
using System.Net.NetworkInformation;
using System.Threading;
using HGJ.ConsoleLib;
using OnkyoISCPlib;
using OnkyoISCPlib.Commands;

namespace ConsoleApplication1 {
  class Program {
    private static bool powerStatus;

    static void Main(string[] args) {
      //Setup console-regions
      HGJConsole.Reset();
      HGJConsole.BackgroundColor = ConsoleColor.Blue;
      HGJConsole.Regions.Add("input1", new ConsoleRegion(new ConsolePoint(1, 1), 3, 35, "Input", true));
      HGJConsole.Regions.Add("recv", new ConsoleRegion(new ConsolePoint(1, 6), 4, 35, "Recieved", false));
      HGJConsole.Regions.Add("status", new ConsoleRegion(new ConsolePoint(1, 12), 2, 35, "Status", true));
      HGJConsole.Regions.Add("menu", new ConsoleRegion(new ConsolePoint(40, 1), 13, 35, "Menu", false));
      HGJConsole.Regions.Add("device", new ConsoleRegion(new ConsolePoint(1, 16), 3, 74, "Device-info", false));
      HGJConsole.Draw(true);

      //Auto-discovery, or get IP by input
      HGJConsole.Regions["status"].WriteContent("Finding reciever...");
      var discovery = ISCPDeviceDiscovery.DiscoverDevice("172.16.40.255", 60128);
      string deviceip = discovery.IP;
      if (deviceip == string.Empty) {
        HGJConsole.Regions["status"].WriteContent("Finding reciever... failed.");
        HGJConsole.Regions["input1"].WriteContent("Please input IP of reciever: ");
        deviceip = HGJConsole.Regions["input1"].GetInput(2);
        HGJConsole.Regions["device"].Visible = true;
        discovery.IP = deviceip;
        discovery.MAC = "N/A";
        discovery.Model = "N/A";
        discovery.Port = 60128;
        discovery.Region = "N/A";
        HGJConsole.Regions["device"].WriteContent(discovery.ToString());
      } else {
        HGJConsole.Regions["status"].WriteContent("Finding reciever... Success.");
        HGJConsole.Regions["device"].Visible = true;
        HGJConsole.Regions["device"].WriteContent(discovery.ToString());
      }

      //Check if host is alive
      Ping p = new Ping();
      PingReply rep = p.Send(deviceip, 3000);
      while (rep.Status != IPStatus.Success) {
        HGJConsole.Regions["status"].WriteContent(string.Format("Cannot connect to Onkyo reciever ({0}). Sleeping 30sec", rep.Status));
        Thread.Sleep(30000);
        p.Send(deviceip, 3000);
      }

      //Setup sockets to reciever
      HGJConsole.Regions["status"].WriteContent("Connecting.");
      ISCPSocket.DeviceIp = discovery.IP;
      ISCPSocket.DevicePort = discovery.Port;
      ISCPSocket.OnPacketRecieved += ISCPSocket_OnPacketRecieved;
      ISCPSocket.StartListener();
      HGJConsole.Regions["status"].WriteContent("Connected!");
      HGJConsole.Regions["recv"].Visible = true;
      HGJConsole.Regions["input1"].WriteContent("Input:\r\n> ");

      //Write menu to console-region
      writeMenu();

      //Loop input characters...
      bool shouldstop = false;
      while (!shouldstop) {
        var cki = HGJConsole.Regions["input1"].GetChar(2);
        if (cki.Modifiers == ConsoleModifiers.Shift) {
          switch (cki.Key) {
            case ConsoleKey.Add:
            case ConsoleKey.OemPlus:
            case ConsoleKey.Subtract:
            case ConsoleKey.OemMinus:
              ISCPSocket.SendPacket(MasterVolume.Status);
              break;
            case ConsoleKey.V:
              ISCPSocket.SendPacket(MasterVolume.Status);
              break;
            case ConsoleKey.P:
              ISCPSocket.SendPacket(Power.Status);
              break;
            case ConsoleKey.M:
              ISCPSocket.SendPacket(Muting.Status);
              break;
            case ConsoleKey.A:
              ISCPSocket.SendPacket(Audio.Status);
              break;
          }
        } else {
          switch (cki.Key) {
            case ConsoleKey.Add:
            case ConsoleKey.OemPlus:
              ISCPSocket.SendPacket(MasterVolume.Up);
              break;
            case ConsoleKey.Subtract:
            case ConsoleKey.OemMinus:
              ISCPSocket.SendPacket(MasterVolume.Down);
              break;
            case ConsoleKey.P:
              ISCPSocket.SendPacket(Power.Status, true);
              ISCPSocket.SendPacket(powerStatus ? Power.Off : Power.On);
              break;
            case ConsoleKey.M:
              ISCPSocket.SendPacket(Muting.Toggle);
              break;
            case ConsoleKey.H:
              ISCPSocket.SendPacket(OSD.Home);
              break;
            case ConsoleKey.UpArrow:
              ISCPSocket.SendPacket(OSD.Up);
              break;
            case ConsoleKey.DownArrow:
              ISCPSocket.SendPacket(OSD.Down);
              break;
            case ConsoleKey.RightArrow:
              ISCPSocket.SendPacket(OSD.Right);
              break;
            case ConsoleKey.LeftArrow:
              ISCPSocket.SendPacket(OSD.Left);
              break;
            case ConsoleKey.X:
              ISCPSocket.SendPacket(OSD.Exit);
              break;
            case ConsoleKey.Enter:
              ISCPSocket.SendPacket(OSD.Enter);
              break;
            case ConsoleKey.Q:
              shouldstop = true;
              break;
          }
        }
      }

      HGJConsole.Regions["status"].WriteContent("... Press any key to exit ...");
      Console.ReadKey();
      ISCPSocket.Dispose();
    }

    static void ISCPSocket_OnPacketRecieved(string str) {
      HGJConsole.Regions["recv"].WriteContent("Recieved: " + str);
      var r = ISCPPacket.ParsePacket(str);
      if (r is Power) {
        powerStatus = (r.Command == "!1PWR01");
      }
      HGJConsole.Regions["recv"].WriteContent(r.ToString(), true);
    }

    private static void writeMenu() {
      if (!HGJConsole.Regions["menu"].Visible) HGJConsole.Regions["menu"].Visible = true;
      HGJConsole.Regions["menu"].WriteContent(@"            Action:     Status:
 Audio-info             Shift A
 Volume     +/-         Shift +/-
 Mute       M           Shift M
 Power      P           Shift P
 Quit       Q

 Home       H
 Exit       X
 Enter      Enter
 Navigate   Arrow-keys

"
        );
    }
  }
}