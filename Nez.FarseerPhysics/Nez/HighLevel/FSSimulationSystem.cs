using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Textures;

namespace Nez.Farseer {


    public class FSSimulationSystem : ProcessingSystem {

        public FSSimulationSystem() : base()
        {
        }

        public override void Process() {
            var world = Scene.GetSceneComponent<FSWorld>();
            if(world == null) return;

            world.Step(Time.DeltaTime);

        }
    }
}