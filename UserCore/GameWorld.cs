
using System.Runtime.CompilerServices;
using Core.ECSS;
using Core.Engine;
using Core.FinderIO;
using Core.InputManager;
using Core.MemoryManagement;
using Core.SAS2D;
using Core.Shimshek;
using Core.Transformations;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using UserCode.PlayerHandling;
using UserCode.WorldHandling;


namespace UserCore;

[Scene]
public unsafe static class GameWorld
{




    private static int player;

    [Start]
    public static void Start()
    {
        new WorldHandler();


        player = ECSSHandler.CreateEntity();

        ECSSHandler.AddComponent(player, new Cuppy());
    }


    [Update]
    public static void Update()
    {
        if(KBMInput.IsHeld((int)Keys.Escape))
            FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowClose, null);
    }
}