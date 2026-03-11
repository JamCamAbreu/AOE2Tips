using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HPScreen.Entities
{
    public abstract class Sprite
    {
        public abstract float SpriteWidth { get; }
        public abstract float SpriteHeight { get; }
        public Sprite()
        {
            Xpos = -SpriteWidth * 2;
            Ypos = 0;
            TargetXPos = -SpriteWidth * 2;
            TargetYPos = 0;
            Scale = 1;
            TargetScale = 1;
            Flipped = false;
        }
        public float Xpos { get; set; }
        public float Ypos { get; set; }
        public float TargetXPos { get; set; }
        public float TargetYPos { get; set; }
        public float Scale { get; set; }
        public float TargetScale { get; set; }
        public bool Flipped { get; set; }
        public void SetAbsolutePosition(float x, float y)
        {
            this.Xpos = x;
            this.Ypos = y;
            this.TargetXPos = x;
            this.TargetYPos = y;
        }
    }
}
