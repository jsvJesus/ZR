package
{
   import communication.Api;
   import communication.Settings;
   import flash.display.Sprite;
   import flash.display.Stage;
   import flash.display.StageAlign;
   import flash.display.StageQuality;
   import flash.display.StageScaleMode;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.external.ExternalInterface;
   import flash.system.Capabilities;
   import flash.ui.Keyboard;
   import flash.utils.setTimeout;
   import lang.Locale;
   import logging.Logger;
   import ui.Navigator;
   
   [SWF(frameRate="60",width="1024",height="768",backgroundColor="0xFF0000")]
   public class Base extends Sprite
   {
      public static var self:Base;
      
      public static var stage:Stage;
      
      public static var logLevel:Sprite;
      
      public static var comboLevel:Sprite;
      
      public static var contentLevel:Sprite;
      
      public static var CalloutLevel:Sprite;
      
      public static var navigator:Navigator;
      
      public static const VERSION:String = "1.1.0";
      
      public static const RESET_KEY_CODE:uint = Keyboard.F11;
      
      public static var USE_FILTERS:Boolean = false;
      
      public static var QUALITY:String = StageQuality.MEDIUM;
      
      public static var ALIGN:String = StageAlign.TOP_LEFT;
      
      public static var SCALEMODE:String = StageScaleMode.NO_SCALE;
      
      public static var LOG_ALPHA:Number = 0.75;
      
      public static var LOG_VISIBLE_ON_START:Boolean = true;
      
      public static var BG_SHIELD_IS_ACTIVE:Boolean = false;
      
      public static var BG_SHIELD_COLOR:int = 16711680;
      
      public static var BG_SHIELD_ALPHA:Number = 0.5;
      
      public static var FONT_LIGHT:String = "Roboto Condensed Light";
      
      public static var FONT_REGULAR:String = "Roboto Condensed Regular";
      
      public static var FONT_BOLD:String = "Roboto Condensed Bold";
      
      public static var FONT_BULLETS:String = "justbullets";
      
      protected var shield:Sprite;
      
      protected var startInitDelay:uint = 200;
      
      protected var initDelay:uint = 50;
      
      protected var initPool:Array;
      
      public function Base()
      {
         super();
         Base.self = this;
         Logger.LogToChannel(Logger.DEFAULT,"MainMenuGUI; version: ",VERSION,";",Capabilities.os,";",Capabilities.playerType,";",Capabilities.version);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
      }
      
      public static function get isScaleform() : Boolean
      {
         return String(Capabilities.version).toLowerCase().indexOf("win") < 0;
      }
      
      private function onAddedToStageHadler(event:Event = null) : void
      {
         Base.self = this;
         Base.stage = this.stage;
         Logger.LogToChannel(Logger.DEBUG,"Base was added to stage");
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
         this.reset();
      }
      
      protected function initNavigator() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"initNavigator..");
         if(!navigator)
         {
            navigator = new Navigator();
            contentLevel.addChild(navigator);
         }
         Logger.LogToChannel(Logger.DEBUG,"initNavigator ok.");
      }
      
      protected function initGUI() : void
      {
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
         this.initPool = [this.initShield,this.initNavigator,this.initGUI,this.initSettings];
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
      
      private function initLocales() : void
      {
         Locale.load(["KeyBinds","Bird","DroppedItem","extendedGUI","DefaultModels","soOptionsGUI","BWPersonality"]);
         Locale.LoadKeybindsLocTable();
      }
      
      private function initSettings() : void
      {
         Settings.Update();
      }
      
      private function initMouse() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"initMouse..");
         try
         {
            Base.stage.addEventListener(MouseEvent.CLICK,this.onSingleClick);
            Base.stage.addEventListener(MouseEvent.MOUSE_UP,this.onSingleMouseUp);
            Base.stage.addEventListener(MouseEvent.MOUSE_DOWN,this.onSingleMouseDown);
            Base.stage.addEventListener(MouseEvent.RIGHT_CLICK,this.onRightClickNow);
            Base.stage.addEventListener(MouseEvent.RIGHT_MOUSE_UP,this.onRightMouseUp);
            Base.stage.addEventListener(MouseEvent.RIGHT_MOUSE_DOWN,this.onRightMouseDown);
            Base.stage.addEventListener(MouseEvent.MIDDLE_CLICK,this.onMiddleClick);
            Base.stage.addEventListener(MouseEvent.MIDDLE_MOUSE_DOWN,this.onMiddleMouseDown);
            Base.stage.addEventListener(MouseEvent.MIDDLE_MOUSE_UP,this.onMiddleMouseUp);
            Base.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMoveNow);
            Base.stage.addEventListener(MouseEvent.MOUSE_WHEEL,this.onMouseWheel);
            Base.stage.doubleClickEnabled = true;
            Base.stage.addEventListener(MouseEvent.DOUBLE_CLICK,this.onDoubleClick);
            Logger.LogToChannel(Logger.DEBUG,"initMouse ok.");
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.DEBUG,"initMouse ALMOST ok.",error.message);
         }
      }
      
      protected function onMiddleMouseUp(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onMiddleMouseUp");
      }
      
      protected function onMiddleMouseDown(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onMiddleMouseDown");
      }
      
      protected function onRightMouseDown(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onRightMouseDown");
      }
      
      protected function onRightMouseUp(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onRightMouseUp");
      }
      
      protected function onDoubleClick(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onDoubleClick");
      }
      
      protected function onMouseMoveNow(event:MouseEvent) : void
      {
      }
      
      protected function onMouseWheel(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onMouseWheel");
      }
      
      protected function onMiddleClick(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onMiddleClick");
      }
      
      protected function onRightClickNow(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onRightClickNow");
      }
      
      protected function onSingleClick(event:MouseEvent) : void
      {
      }
      
      protected function onSingleMouseDown(event:MouseEvent) : void
      {
      }
      
      protected function onSingleMouseUp(event:MouseEvent) : void
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
      
      protected function initKeyboard() : void
      {
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function initStage() : void
      {
         Base.self.stage.align = ALIGN;
         Base.self.stage.scaleMode = SCALEMODE;
         Logger.LogToChannel(Logger.DEBUG,"QUALITY: ",QUALITY,"; ALIGN: ",ALIGN,"; SCALEMODE:",SCALEMODE,";");
         Base.self.stage.removeEventListener(Event.RESIZE,this.onStageResize);
         Base.self.stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         Base.self.stage.addEventListener(Event.RESIZE,this.onStageResize);
         Base.self.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         Base.self.stage.addChild(Base.contentLevel = new Sprite());
         Base.self.stage.addChild(Base.comboLevel = new Sprite());
         Base.self.stage.addChild(Base.CalloutLevel = new Sprite());
         Base.self.stage.addChild(Base.logLevel = new Sprite());
      }
      
      protected function onMouseMove(event:MouseEvent) : void
      {
      }
      
      protected function onStageResize(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"stage resize:",this.stage.stageWidth,this.stage.stageHeight);
         this.redrawShield();
      }
      
      protected function initShield() : void
      {
         if(!BG_SHIELD_IS_ACTIVE)
         {
            return;
         }
         if(Boolean(this.shield))
         {
            this.removeChild(this.shield);
            this.shield = null;
         }
         this.shield = new Sprite();
         this.addChild(this.shield);
         this.redrawShield();
      }
      
      protected function redrawShield() : void
      {
         if(Boolean(this.shield))
         {
            this.shield.graphics.clear();
            this.shield.graphics.beginFill(BG_SHIELD_COLOR,BG_SHIELD_ALPHA);
            this.shield.graphics.drawRect(0,0,this.stage.stageWidth,this.stage.stageHeight);
            this.shield.graphics.endFill();
         }
      }
      
      protected function initLogger() : void
      {
         Logger.init(Base.logLevel,true);
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
      }
      
      protected function test() : void
      {
      }
      
      public function doLogin(loginPack:Object) : void
      {
      }
   }
}

