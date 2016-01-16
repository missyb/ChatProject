using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_client
{
    public static class Images
    {
        
          public static Image GetImage(string imgCode)
          {
              Image img = null;

              switch (imgCode)
              {
                  case "EmbarassedSmile":
                      img = Properties.Resources.EmbarassedSmile;
                      return img;
                     
                  case "AngelSmile":
                      img = Properties.Resources.AngelSmile;
                      return img;
                     
                  case "AngrySmile":
                      img = Properties.Resources.AngrySmile;
                      return img;

                  case "Beer":
                      img = Properties.Resources.Beer;
                      return img;

                  case "BrokenHeart":
                      img = Properties.Resources.BrokenHeart;
                      return img;

                  case "ConfusedSmile":
                      img = Properties.Resources.ConfusedSmile;
                      return img;

                  case "CrySmile":
                      img = Properties.Resources.CrySmile;
                      return img;

                  case "DevilSmile":
                      img = Properties.Resources.DevilSmile;
                      return img;

                  case "ThumbsUp":
                      img = Properties.Resources.ThumbsUp;
                      return img;

                  case "black_eye":
                      img = Properties.Resources.black_eye;
                      return img;
                      
                  case "slapping":
                      img = Properties.Resources.slapping;
                      return img;
                      
                  case "shit_emoticon":
                      img = Properties.Resources.shit_emoticon;
                      return img;

                  default: 
                      img = Properties.Resources.redlightc;
                      return img;
              }
          }

     
    }
}
