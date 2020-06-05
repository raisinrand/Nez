using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Textures;

namespace Nez.Farseer {


    public class FSBodyUpdateSystem : EntityProcessingSystem {

        public FSBodyUpdateSystem() : base(
            new Matcher().All(
                typeof(FSRigidBody)
        )){}

        public override void Process(Entity entity) {
            var rb = entity.GetComponent<FSRigidBody>();
            rb.UpdateTransforms();
        }
    }
}