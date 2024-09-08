using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;


namespace LocalRespawns;

public class LocalRespawnsModSystem : ModSystem
{
    struct PlayerSpawnInfo
    {
        public EntityPos position;
        public IPlayer player;
    }
    private ICoreServerAPI _sapi = null!;

    private int spawnRadi = 10;

    private int worldHeight = 256;
    

    private List<PlayerSpawnInfo> playerSpawnInfos = new List<PlayerSpawnInfo>();
    
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {

        spawnRadi = api.World.Config.GetAsInt("spawnRadius", 100);
        //worldHeight = api.World.Config.GetAsInt("MapSizeY", 256);
        Mod.Logger.Notification("Set Local Respawn Radius to world config respawn radius: " +spawnRadi.ToString());
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        _sapi = api;
        api.Event.OnEntityDeath += OnEntityDeath;
        //api.Event.PlayerRespawn += OnPR;
        

    }

    private void OnEntityDeath(Entity entity, DamageSource damageSource)
    {
        if (entity is EntityPlayer entityPlayer)
        {
            OnPlayerDeath((IServerPlayer)entityPlayer.Player, damageSource);
        }
    }

    private void OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
    {
        var loc = byPlayer.Entity.ServerPos;
        Vec3i newLocation = GenerateSpawnLocation(loc);
        byPlayer.SetSpawnPosition(new PlayerSpawnPos(newLocation.X, newLocation.Y, newLocation.Z));
        //PlayerSpawnInfo pSI = new PlayerSpawnInfo();
        //pSI.position = loc;
        //pSI.player = byPlayer;
        //playerSpawnInfos.Add(pSI);

    }

    /*private void OnPR(IServerPlayer player)
    {
        for (int entry = 0; entry < playerSpawnInfos.Count; entry++)
        {
            if (playerSpawnInfos[entry].player == player)
            {
                EntityPos newLocation = GenerateSpawnLocation(playerSpawnInfos[entry].position);
                player.Entity.Pos.SetPos(newLocation);
                Mod.Logger.Notification("Local Respawn Location (Post Respawn): " + newLocation);
                Mod.Logger.Notification("Entity Location: " + player.Entity.Pos);
                playerSpawnInfos.RemoveAt(entry);
                return;
            }
        }
    } */

    private Vec3i GenerateSpawnLocation(EntityPos pos)
    {
        Mod.Logger.Notification("Initial Position for respawn:" + pos);
        Random rnd = new Random();
        int posNeg = rnd.NextDouble() < 0.5 ? -1 : 1;
        double randx = rnd.NextDouble() * spawnRadi * posNeg;
        posNeg = rnd.NextDouble() < 0.5 ? -1 : 1;
        double randz = rnd.NextDouble() * spawnRadi * posNeg;
        EntityPos NewPosition = pos.Copy();
        NewPosition.X += randx;
        NewPosition.Z += randz;
        Mod.Logger.Notification("Generating New Position: " + NewPosition);
        BlockPos floorPos = findFloor(NewPosition.AsBlockPos);

        Vec3i newPos = floorPos.ToVec3i();

        /*NewPosition.X = newPos.X;
        NewPosition.Y = newPos.Y;
        NewPosition.Z = newPos.Z;
        
        Mod.Logger.Notification("New Position: " + NewPosition);*/
        
        return newPos;
    }

    private BlockPos findFloor(BlockPos origin)
    {
        var floorPos = new BlockPos(origin.dimension);

        for (int i = worldHeight - 1; i > 0; i--)
        {
            floorPos.Set(origin.X, i, origin.Z);
            var block = _sapi.World.BlockAccessor.GetBlock(floorPos);
            if (block.BlockId != 0 && block.CollisionBoxes?.Length > 0)
            {
                floorPos.Set(origin.X, i + 1, origin.Z);
                return floorPos;
            }
        }

        return origin;
    }
}