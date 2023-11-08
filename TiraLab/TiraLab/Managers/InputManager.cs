﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiraLab.Managers;

namespace TiraLab
{
    public class InputManager
    {
        CommandManager _commandManager;
        public InputManager()
        {
            _commandManager = new CommandManager();
        }
        public void ReadInput()
        {
            string userInput = Console.ReadLine();
            if (userInput == null)
            {
                return;
            }
            _commandManager.ProcessInput(userInput);
        }

    }
}