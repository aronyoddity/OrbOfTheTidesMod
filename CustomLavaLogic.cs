using System;
 using System.Collections.Generic;
 using Microsoft.Xna.Framework;
 using Microsoft.Xna.Framework.Graphics;
 using Netcode;
 using StardewValley.Extensions;
 using StardewValley.Monsters;
 using StardewValley.Network;
 using StardewValley.Objects;
 using xTile.Dimensions;
 using xTile.Layers;
 using xTile.Tiles;
 using StardewModdingAPI;
 using StardewValley;
 using StardewValley.Tools;
 using System.Xml.Serialization;
 using SpaceShared.APIs;
 using StardewModdingAPI.Events;
using StardewValley.BellsAndWhistles;
using StardewValley.TerrainFeatures;
using xTile;


 namespace CapeStardewCode 
 
 { [XmlType($"Mods_{nameof(CustomLavaLogic)}")] [XmlInclude(typeof(CustomLavaLogic))] 
 public class CustomLavaLogic : GameLocation 
 { 
 [XmlIgnore] public NetEvent1Field<Point, NetPoint> coolLavaEvent = new NetEvent1Field<Point, NetPoint>();
 [XmlIgnore] public NetVector2Dictionary<bool, NetBool> cooledLavaTiles = new NetVector2Dictionary<bool, NetBool>();
 [XmlIgnore] public Dictionary<Vector2, Point> localCooledLavaTiles = new Dictionary<Vector2, Point>();
  [XmlIgnore] private Texture2D? mapBaseTilesheet;
 protected static Dictionary<int, Point>? _blobIndexLookup = null;
  protected static Dictionary<int, Point>? _lavaBlobIndexLookup = null;
  private int lavaSoundsPlayedThisTick;
 //calderalogic 
 private readonly IModHelper? _helper;
 private readonly IMonitor? _monitor;
  private readonly string? _mapPath;
  private readonly string? _locationName;

 


 public CustomLavaLogic() : base() // Call base constructor if applicable
  { } 
  public CustomLavaLogic(string mapPath, string name) : base(mapPath, name) 
  { // Initialize mapBaseTilesheet (you may want to load an actual texture here later) 
  mapBaseTilesheet = Game1.temporaryContent.Load<Texture2D>("Maps/Mines/volcano_dungeon");
 // Or some default value like Game1.temporaryContent.Load<Texture2D>("defaultPath");
 } 
 
 public CustomLavaLogic(IModHelper helper, IMonitor monitor, string mapPath, string locationName) 
    : base(mapPath, locationName)
{
    _helper = helper ?? throw new ArgumentNullException(nameof(helper));
    _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    _mapPath = mapPath ?? throw new ArgumentNullException(nameof(mapPath));
    _locationName = locationName ?? throw new ArgumentNullException(nameof(locationName));

    // Load the map
    loadMap(_mapPath);

    try
    {
        mapBaseTilesheet = Game1.temporaryContent.Load<Texture2D>("Maps/Mines/volcano_dungeon");
        if (mapBaseTilesheet != null)
        {
            throw new Exception("mapBaseTilesheet is null after loading texture.");
        }
    }
    catch (Exception ex)
    {
        _monitor.Log($"Error loading texture for mapBaseTilesheet: {ex.Message}", LogLevel.Error);
    }

    waterColor.Value = Color.Red;
}
  public override bool BlocksDamageLOS(int x, int y)
        {
            return !cooledLavaTiles.ContainsKey(new Vector2(x, y)) && base.BlocksDamageLOS(x, y);
        }

         protected override void initNetFields() 
 
 { 
    base.initNetFields();
 base.NetFields.AddField(coolLavaEvent,
  "coolLavaEvent").AddField(cooledLavaTiles.NetFields,
   "cooledLavaTiles.NetFields");
 coolLavaEvent.onEvent += OnCoolLavaEvent;
 } 

 private CustomLavaLogic? GetCustomLavaLocation() 
 { 
    GameLocation location = Game1.getLocationFromName(_locationName ?? "CapeKeyMaze");
 if (location is CustomLavaLogic customLavaLocation) 
 { 
    return customLavaLocation;
 }
  _monitor?.Log($"Failed to cast {_locationName} to CustomLavaLogic.", LogLevel.Error);
 return null;
 } // Overriding resetLocalState to handle lava tiles 
 



 
 public virtual void OnCoolLavaEvent(Point point) 
 { 
 CoolLava(point.X, point.Y);
 UpdateLavaNeighbor(point.X, point.Y);
 UpdateLavaNeighbor(point.X - 1, point.Y);
 UpdateLavaNeighbor(point.X + 1, point.Y);
 UpdateLavaNeighbor(point.X, point.Y - 1);
 UpdateLavaNeighbor(point.X, point.Y + 1);
 } 
 
 public virtual void CoolLava(int x, int y, bool playSound = true) 
 { 
    if (Game1.currentLocation == this) { for (int i = 0; i < 5; i++) 
 { 
   temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Vector2(x, (float)y - 0.5f) * 64f + new Vector2(Game1.random.Next(64), Game1.random.Next(64)), flipped: false, 0.007f, Color.White)
   {
						alpha = 0.75f,
						motion = new Vector2(0f, -1f),
						acceleration = new Vector2(0.002f, 0f),
						interval = 99999f,
						layerDepth = 1f,
						scale = 4f,
						scaleChange = 0.02f,
						rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
						delayBeforeAnimationStart = i * 35
					});
 } 
 if (playSound && lavaSoundsPlayedThisTick < 3) 
 { DelayedAction.playSoundAfterDelay("steam", lavaSoundsPlayedThisTick * 300);
  lavaSoundsPlayedThisTick++;
 }
  
  } 
  cooledLavaTiles.TryAdd(new Vector2(x, y), value: true);
 } 
 
 public Dictionary<int, Point> GetBlobLookup() 
 { 
    if (_blobIndexLookup == null) 
    { _blobIndexLookup = new Dictionary<int, Point>();
 _blobIndexLookup[0] = new Point(0, 0);
 _blobIndexLookup[6] = new Point(1, 0);
 _blobIndexLookup[14] = new Point(2, 0);
 _blobIndexLookup[10] = new Point(3, 0);
 _blobIndexLookup[7] = new Point(1, 1);
 _blobIndexLookup[11] = new Point(3, 1);
 _blobIndexLookup[5] = new Point(1, 2);
 _blobIndexLookup[13] = new Point(2, 2);
 _blobIndexLookup[9] = new Point(3, 2);
 _blobIndexLookup[2] = new Point(0, 1);
 _blobIndexLookup[3] = new Point(0, 2);
 _blobIndexLookup[1] = new Point(0, 3);
 _blobIndexLookup[4] = new Point(1, 3);
 _blobIndexLookup[12] = new Point(2, 3);
 _blobIndexLookup[8] = new Point(3, 3);
 _blobIndexLookup[15] = new Point(2, 1);
 } 
 return _blobIndexLookup;
 } 
 
 public Dictionary<int, Point> GetLavaBlobLookup() 
 { if (_lavaBlobIndexLookup == null) 
 { _lavaBlobIndexLookup = new Dictionary<int, Point>(GetBlobLookup());
 _lavaBlobIndexLookup[63] = new Point(2, 1);
 _lavaBlobIndexLookup[47] = new Point(4, 3);
 _lavaBlobIndexLookup[31] = new Point(4, 2);
 _lavaBlobIndexLookup[15] = new Point(4, 1);
 } 
 return _lavaBlobIndexLookup;
 } 
 
 
 public virtual void UpdateLavaNeighbor(int x, int y) 
 { if (IsCooledLava(x, y)) 
 { 
 setTileProperty(x, y, "Buildings", "Passable", "T");
 int neighbors = 0;
 if (IsCooledLava(x, y - 1)) { neighbors++;} 
 if (IsCooledLava(x, y + 1)) { neighbors += 2;} 
 if (IsCooledLava(x - 1, y)) { neighbors += 8;} 
 if (IsCooledLava(x + 1, y)) { neighbors += 4;} 
 if (GetBlobLookup().TryGetValue(neighbors, out var offset)) 
 { 
    localCooledLavaTiles[new Vector2(x, y)] = offset;
 } 
 } 
 }
 
  public override void checkForMusic(GameTime time) 
  { 
    if (Game1.getMusicTrackName() == "shoesbyMyca" || Game1.isMusicContextActiveButNotPlaying()) 
  { 
    Game1.changeMusicTrack("Volcano_Ambient");
 } 
 base.checkForMusic(time);
 } 
 
 
 public virtual bool IsCooledLava(int x, int y) 
 { 
 return cooledLavaTiles.ContainsKey(new Vector2(x, y));
 } 
 
 protected override void resetSharedState()
		{
			base.resetSharedState();
			{
				waterColor.Value = Color.White;
			}
		}

 public bool isTileOnClearAndSolidGround(Vector2 v) 
 
 { 
    if (map.RequireLayer("Back").Tiles[(int)v.X, (int)v.Y] == null) 
 { 
    return false;
 } 
 if (map.RequireLayer("Front").Tiles[(int)v.X, (int)v.Y] != null || map.RequireLayer("Buildings").Tiles[(int)v.X, (int)v.Y] != null) { return false;
 } 
 return true;
 } 
 
 public override bool sinkDebris(Debris debris, Vector2 chunkTile, Vector2 chunkPosition) 
 { 
    if (cooledLavaTiles.ContainsKey(chunkTile)) 
    { 
        return false;
 } 
 return base.sinkDebris(debris, chunkTile, chunkPosition);
 } 
 

 // Disabling refilling watering cans on lava tiles 
 public override bool CanRefillWateringCanOnTile(int tileX, int tileY) 
 { 
    return false;
 // No refilling on lava tiles 
 } 
 

 
 

 
 public static int GetTileIndex(int x, int y) 
 { 
    return x + (y * 16);
 } 
 
 public void SetTile(Layer layer, int x, int y, int index) 
 { 
    if (x >= 0 && x < layer.LayerWidth && y >= 0 && y < layer.LayerHeight) 
    { Location location = new Location(x, y);
 layer.Tiles[location] = new StaticTile(layer, map.TileSheets[0], BlendMode.Alpha, index);
 } 
 
 } 
 
 public virtual void CleanUp() 
 
 { 
    if (!Game1.IsMasterGame) 
    { 
        return;
 } 
 int i = 0;
 while (i < debris.Count) { Debris d = debris[i];
 if (d.isEssentialItem() && Game1.IsMasterGame && d.collect(Game1.player)) 
 { 
    debris.RemoveAt(i);
 } 
 else { i++;
 } 
 } 
 } 
 
 public override bool ShouldExcludeFromNpcPathfinding() 
 
 { 
    return true;
 } 
 
 
public override void drawWaterTile(SpriteBatch b, int x, int y)
{
   // Ensure mapBaseTilesheet is not null before drawing
    if (mapBaseTilesheet != null)
    {
    bool num = y == map.Layers[0].LayerHeight - 1 || !waterTiles[x, y + 1];
			bool topY = y == 0 || !waterTiles[x, y - 1];
			int water_tile_upper_left_x = 0;
			int water_tile_upper_left_y = 320;
			b.Draw(mapBaseTilesheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - (int)((!topY) ? waterPosition : 0f))), new Microsoft.Xna.Framework.Rectangle(water_tile_upper_left_x + waterAnimationIndex * 16, water_tile_upper_left_y + (((x + y) % 2 != 0) ? ((!waterTileFlip) ? 32 : 0) : (waterTileFlip ? 32 : 0)) + (topY ? ((int)waterPosition / 4) : 0), 16, 16 + (topY ? ((int)(0f - waterPosition) / 4) : 0)), waterColor.Value, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.56f);
			if (num)
			{
				b.Draw(mapBaseTilesheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (y + 1) * 64 - (int)waterPosition)), new Microsoft.Xna.Framework.Rectangle(water_tile_upper_left_x + waterAnimationIndex * 16, water_tile_upper_left_y + (((x + (y + 1)) % 2 != 0) ? ((!waterTileFlip) ? 32 : 0) : (waterTileFlip ? 32 : 0)), 16, 16 - (int)(16f - waterPosition / 4f) - 1), waterColor.Value, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.56f);
         }
    }
    else
    {
        // Log or handle the null case for troubleshooting if mapBaseTilesheet is unexpectedly null
        Console.WriteLine("Warning: mapBaseTilesheet is null in drawWaterTile.");
    }
}

 public override bool performToolAction(Tool t, int tileX, int tileY)
		{
			if (t is WateringCan && isTileOnMap(new Vector2(tileX, tileY)) && waterTiles[tileX, tileY] && !cooledLavaTiles.ContainsKey(new Vector2(tileX, tileY)))
			{
				coolLavaEvent.Fire(new Point(tileX, tileY));
			}
			return base.performToolAction(t, tileX, tileY);
		}
 
 public override void UpdateWhenCurrentLocation(GameTime time)
		{
			base.UpdateWhenCurrentLocation(time);
			coolLavaEvent.Poll();
			lavaSoundsPlayedThisTick = 0;
			
				}
			
			
			
		
 public override void drawFloorDecorations(SpriteBatch b)
		{
          if (mapBaseTilesheet != null)
          {
			base.drawFloorDecorations(b);
			for (int y = Game1.viewport.Y / 64 - 1; y < (Game1.viewport.Y + Game1.viewport.Height) / 64 + 1; y++)
			{
				for (int x = Game1.viewport.X / 64 - 1; x < (Game1.viewport.X + Game1.viewport.Width) / 64 + 1; x++)
				{
					Vector2 tile = new Vector2(x, y);
					if (localCooledLavaTiles.TryGetValue(tile, out var point))
					{
						point.X += 5;
						point.Y += 16;
						b.Draw(mapBaseTilesheet, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64)), new Microsoft.Xna.Framework.Rectangle(point.X * 16, point.Y * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.55f);
					}
				}
			}
		}
      }
 
 public override void draw(SpriteBatch b) 
 { base.draw(b);
 
  }
   } 
   }