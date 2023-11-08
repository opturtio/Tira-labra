﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiraLab.Managers;

namespace TiraLab.UI
{
    public class UI
    {
        private OutputManager _outputManager;

        public UI()
        {
            // Instantiates the OutputManager
            _outputManager = new OutputManager();
        }

        public void WelcomeText()
        {
            _outputManager.PrintText("WelcomeText");
        }

        public void MainText()
        {
            _outputManager.PrintText("MainText");
        }
    }
}