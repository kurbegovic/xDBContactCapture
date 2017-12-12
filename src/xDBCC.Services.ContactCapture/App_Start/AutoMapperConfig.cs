using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using xDBCC.Services.ContactCapture.Models;

namespace xDBCC.Services.ContactCapture.App_Start
{
    public class AutoMapperConfig
    {
        public static void Initialize()
        {
            Mapper.Initialize(cfg =>
                {
                    cfg.CreateMap<ReceivedContact, Contact>();
                });
        }
    }
}