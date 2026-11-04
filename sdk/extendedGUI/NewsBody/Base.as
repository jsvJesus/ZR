package
{
   import com.greensock.plugins.HexColorsPlugin;
   import com.greensock.plugins.TweenPlugin;
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
   import flash.text.Font;
   import flash.ui.Keyboard;
   import flash.utils.setTimeout;
   import lang.Locale;
   import logging.Logger;
   import ui.Navigator;
   
   [SWF(frameRate="60",width="1024",height="768",backgroundColor="0xFF0000")]
   public class Base extends Sprite
   {
      public static var FONT_LIGHT:String;
      
      public static var FONT_REGULAR:String;
      
      public static var FONT_BOLD:String;
      
      public static var FONT_BULLETS:String;
      
      public static var Light:Font;
      
      public static var Regular:Font;
      
      public static var Bold:Font;
      
      public static var Bullets:Font;
      
      public static var self:Base;
      
      public static var stage:Stage;
      
      public static var logLevel:Sprite;
      
      public static var comboLevel:Sprite;
      
      public static var contentLevel:Sprite;
      
      public static var CalloutLevel:Sprite;
      
      public static var navigator:Navigator;
      
      public static const DEBUG:Boolean = false;
      
      public static const VERSION:String = "1.2.150611";
      
      public static const RESET_KEY_CODE:uint = Keyboard.F11;
      
      public static const STAGE_RESIZE:String = "overrided_stage_resize";
      
      public static var USE_FILTERS:Boolean = false;
      
      public static var QUALITY:String = StageQuality.MEDIUM;
      
      public static var ALIGN:String = StageAlign.TOP_LEFT;
      
      public static var SCALEMODE:String = StageScaleMode.NO_SCALE;
      
      public static var LOG_ALPHA:Number = 0.75;
      
      public static var LOG_VISIBLE_ON_START:Boolean = true;
      
      public static var BG_SHIELD_IS_ACTIVE:Boolean = false;
      
      public static var BG_SHIELD_COLOR:int = 16711680;
      
      public static var BG_SHIELD_ALPHA:Number = 0.5;
      
      protected var shield:Sprite;
      
      protected var startInitDelay:uint = 200;
      
      protected var initDelay:uint = 50;
      
      protected var initPool:Array;
      
      public function Base()
      {
         super();
         TweenPlugin.activate([HexColorsPlugin]);
         Base.self = this;
         Logger.LogToChannel(Logger.DEFAULT,"MainMenuGUI; version: ",VERSION,";",Capabilities.os,";",Capabilities.playerType,";",Capabilities.version);
         this.prepareFonts();
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
      }
      
      public static function get isScaleform() : Boolean
      {
         return String(Capabilities.version).toLowerCase().indexOf("win") < 0;
      }
      
      protected function prepareFonts() : void
      {
         Light = new GUILightClass();
         FONT_LIGHT = Light.fontName;
         Logger.LogToChannel(Logger.DEFAULT,"SWC light font:",Light.fontName,Light.fontStyle,Light.fontType);
         Regular = new GUIRegularClass();
         FONT_REGULAR = Regular.fontName;
         Logger.LogToChannel(Logger.DEFAULT,"SWC regular font:",Regular.fontName,Regular.fontStyle,Regular.fontType);
         Bold = new GUIBoldClass();
         FONT_BOLD = Bold.fontName;
         Logger.LogToChannel(Logger.DEFAULT,"SWC bold font:",Bold.fontName,Bold.fontStyle,Bold.fontType);
         Bullets = new JustBulletsClass();
         FONT_BULLETS = Bullets.fontName;
         Logger.LogToChannel(Logger.DEFAULT,"SWC bullets font:",Bullets.fontName,Bullets.fontStyle,Bullets.fontType);
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
            Logger.LogToChannel(Logger.DEBUG,"initNavigator ok.");
            contentLevel.addChild(navigator);
            Logger.LogToChannel(Logger.DEBUG,"initNavigator added to displayObjects.");
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
         this.initPool = [this.initNavigator,this.initGUI,this.initSettings];
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
         Locale.load(["KeyBinds","Bird","DroppedItem","extendedGUI","DefaultModels","soOptionsGUI","BWPersonality","EULA"]);
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
         Base.stage.align = ALIGN;
         Base.stage.scaleMode = SCALEMODE;
         Logger.LogToChannel(Logger.DEBUG,"QUALITY: ",QUALITY,"; ALIGN: ",ALIGN,"; SCALEMODE:",SCALEMODE,";");
         Base.stage.removeEventListener(Event.RESIZE,this.onStageResize);
         Base.stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         Base.stage.addEventListener(Event.RESIZE,this.onStageResize);
         Base.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         Base.stage.addChild(Base.contentLevel = new Sprite());
         Base.stage.addChild(Base.comboLevel = new Sprite());
         Base.stage.addChild(Base.CalloutLevel = new Sprite());
         Base.stage.addChild(Base.logLevel = new Sprite());
      }
      
      protected function onMouseMove(event:MouseEvent) : void
      {
      }
      
      protected function onStageResize(event:Event) : void
      {
         trace("Base.onStageResize");
         setTimeout(Base.stage.dispatchEvent,0,new Event(Base.STAGE_RESIZE));
      }
      
      protected function initLogger() : void
      {
         if(DEBUG)
         {
            Logger.init(Base.logLevel,true);
         }
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

