using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class PlayingNow
    {
        public Sound Sound;
        public FromUser User;

        public class FromUser
        {
            public string Name;

            public FromUser(Users.User user)
            {
                //TODO
            }
        }
    }
}
