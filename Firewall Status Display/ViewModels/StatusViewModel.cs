﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls;
using IServiceProvider = System.IServiceProvider;

namespace Firewall_Status_Display.ViewModels
{
    public class StatusViewModel : ViewModelBase
    {
        public string Content { get; set; }

        public StatusViewModel()
        {
            Content = "Status view";
        }
    }
}