package
{
   import communication.Api;
   import communication.Settings;
   import flash.display.Sprite;
   import flash.display.Stage;
   import flash.display.StageAlign;
   import flash.display.StageScaleMode;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.external.ExternalInterface;
   import flash.utils.setTimeout;
   import lang.Locale;
   import logging.Logger;
   
   [SWF(frameRate="60",width="1024",height="768",backgroundColor="0x3995b0")]
   public class GameGUI extends Sprite
   {
      public static var self:GameGUI;
      
      public static var stage:Stage;
      
      public static var logLevel:Sprite;
      
      public static var comboLevel:Sprite;
      
      public static var contentLevel:Sprite;
      
      public static var CalloutLevel:Sprite;
      
      public static var ALIGN:String = StageAlign.TOP_LEFT;
      
      public static var SCALEMODE:String = StageScaleMode.NO_SCALE;
      
      public static var BG_SHIELD_IS_ACTIVE:Boolean = false;
      
      public static var BG_SHIELD_COLOR:int = 16711680;
      
      public static var BG_SHIELD_ALPHA:Number = 0.5;
      
      protected var shield:Sprite;
      
      protected var startInitDelay:uint = 200;
      
      protected var initDelay:uint = 50;
      
      protected var initPool:Array;
      
      public function GameGUI()
      {
         super();
         GameGUI.self = this;
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
      }
      
      protected function onAddedToStageHadler(event:Event) : void
      {
         GameGUI.self = this;
         GameGUI.stage = this.stage;
         Logger.LogToChannel(Logger.DEBUG,"GameGUI was added to stage");
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
         this.reset();
      }
      
      public function reset() : void
      {
         this.initFirst();
      }
      
      public function initFirst() : void
      {
         this.initPool = new Array();
         this.initPool = [this.initStage,this.initLogger,this.initKeyboard,this.initMouse,this.initCommunication,this.initLocales];
         Locale.onSuccess = this.initSecond;
         setTimeout(this.initNext,this.startInitDelay);
      }
      
      public function initSecond() : void
      {
         this.initPool = new Array();
         this.initPool = [this.initSettings];
         setTimeout(this.initNext,this.startInitDelay);
      }
      
      private function initNext() : void
      {
         var initFunction:Function = null;
         if(Boolean(this.initPool.length))
         {
            initFunction = this.initPool.shift();
            Logger.LogToChannel(Logger.DEBUG,initFunction);
            initFunction();
            setTimeout(this.initNext,this.initDelay);
         }
      }
      
      protected function initStage() : void
      {
         GameGUI.self.stage.align = ALIGN;
         GameGUI.self.stage.scaleMode = SCALEMODE;
         GameGUI.self.stage.removeEventListener(Event.RESIZE,this.onStageResize);
         GameGUI.self.stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         GameGUI.self.stage.addEventListener(Event.RESIZE,this.onStageResize);
         GameGUI.self.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         GameGUI.self.stage.addChild(GameGUI.contentLevel = new Sprite());
         GameGUI.self.stage.addChild(GameGUI.comboLevel = new Sprite());
         GameGUI.self.stage.addChild(GameGUI.CalloutLevel = new Sprite());
         GameGUI.self.stage.addChild(GameGUI.logLevel = new Sprite());
      }
      
      protected function onStageResize(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"stage resize:",GameGUI.stage.stageWidth,GameGUI.stage.stageHeight);
         this.redrawShield();
      }
      
      protected function onMouseMove(event:MouseEvent) : void
      {
      }
      
      protected function redrawShield() : void
      {
         if(Boolean(this.shield))
         {
            this.shield.graphics.clear();
            this.shield.graphics.beginFill(BG_SHIELD_COLOR,BG_SHIELD_ALPHA);
            this.shield.graphics.drawRect(0,0,GameGUI.stage.stageWidth,GameGUI.stage.stageHeight);
            this.shield.graphics.endFill();
         }
      }
      
      protected function initLogger() : void
      {
         Logger.init(GameGUI.logLevel,true);
      }
      
      protected function initKeyboard() : void
      {
         GameGUI.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         GameGUI.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
      }
      
      private function initMouse() : void
      {
      }
      
      private function initCommunication() : void
      {
         if(ExternalInterface.available)
         {
            try
            {
               ExternalInterface.marshallExceptions = true;
               ExternalInterface.addCallback("externalInterfaceTransmit",this.externalInterfaceTransmit);
               Logger.LogToChannel(Logger.DEBUG,"ExternalInterface addCallback ok.");
            }
            catch(error:Error)
            {
               Logger.LogToChannel(Logger.ERROR,"Error while ExternalInterface callback initializing:",error.message);
            }
         }
         else
         {
            Logger.LogToChannel(Logger.ERROR,"External Interface is not available on initCommunication!");
         }
      }
      
      private function externalInterfaceTransmit(response:String) : void
      {
         Api.onCallBack(response);
      }
      
      private function initLocales() : void
      {
         Locale.load(["KeyBinds","Bird","DroppedItem","extendedGUI","DefaultModels","soOptionsGUI","BWPersonality"]);
      }
      
      private function initSettings() : void
      {
         Settings.Update();
      }
   }
}

