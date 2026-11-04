package
{
   import communication.*;
   import flash.display.*;
   import flash.events.*;
   import flash.external.*;
   import flash.net.*;
   import flash.system.*;
   import flash.text.*;
   import flash.ui.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.*;
   
   public class Base extends Sprite
   {
      public static var background:Bitmap;
      
      public static var FONT_LIGHT:String;
      
      public static var FONT_REGULAR:String;
      
      public static var FONT_BOLD:String;
      
      public static var FONT_BULLETS:String;
      
      public static var Light:Font;
      
      public static var Regular:Font;
      
      public static var Bold:Font;
      
      public static var self:Base;
      
      public static var stage:Stage;
      
      public static var logLevel:Sprite;
      
      public static var comboLevel:Sprite;
      
      public static var contentLevel:Sprite;
      
      public static var CalloutLevel:Sprite;
      
      public static var navigator:Navigator;
      
      public static var Bullets:Font;
      
      public static const RESET_KEY_CODE:uint = Keyboard.F11;
      
      public static const STAGE_RESIZE:String = "overrided_stage_resize";
      
      public static const VERSION:String = "1.2.150706";
      
      public static var DEBUG:Boolean = false;
      
      public static var IN_GAME:Boolean = false;
      
      public static var USE_FILTERS:Boolean = false;
      
      public static var QUALITY:String = "medium";
      
      public static var ALIGN:String = "TL";
      
      public static var SCALEMODE:String = "noScale";
      
      public static var LOG_ALPHA:Number = 0.75;
      
      public static var LOG_VISIBLE_ON_START:Boolean = true;
      
      public static var BG_SHIELD_IS_ACTIVE:Boolean = false;
      
      public static var BG_SHIELD_COLOR:int = 16711680;
      
      public static var BG_SHIELD_ALPHA:Number = 0.5;
      
      DEBUG = false;
      USE_FILTERS = false;
      QUALITY = StageQuality.MEDIUM;
      ALIGN = StageAlign.TOP_LEFT;
      SCALEMODE = StageScaleMode.NO_SCALE;
      LOG_ALPHA = 0.75;
      LOG_VISIBLE_ON_START = true;
      BG_SHIELD_IS_ACTIVE = false;
      BG_SHIELD_COLOR = 16711680;
      BG_SHIELD_ALPHA = 0.5;
      
      public var myLoader:Loader = new Loader();
      
      private var url:URLRequest = new URLRequest("../soGUI/maps/Login/login_bg.jpg");
      
      protected var X:Number;
      
      protected var Y:Number;
      
      protected var shield:Sprite;
      
      protected var startInitDelay:uint = 0;
      
      protected var initDelay:uint = 0;
      
      protected var initPool:Array;
      
      public function Base()
      {
         super();
         this.prepareFonts();
         this.myLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,this.doneLoad);
         this.myLoader.load(this.url);
         PremiumIcons.LoadSettingsBackGround();
         this.debugControl();
         Base.self = this;
         Logger.LogToChannel(Logger.DEFAULT,"MainMenuGUI; version: ",VERSION,";",Capabilities.os,";",Capabilities.playerType,";",Capabilities.version);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
      }
      
      public static function get isScaleform() : Boolean
      {
         return String(Capabilities.version).toLowerCase().indexOf("win") < 0;
      }
      
      protected function test() : void
      {
      }
      
      public function doLogin(param1:Object) : void
      {
      }
      
      protected function debugControl() : void
      {
         Base.DEBUG = false;
         Dummy.DEBUG = false;
      }
      
      protected function prepareFonts() : void
      {
         Light = new GUILight();
         FONT_LIGHT = "GUILight";
         Regular = new GUIRegular();
         FONT_REGULAR = "GUIRegular";
         Bold = new GUIBold();
         FONT_BOLD = "GUIBold";
         Bullets = new JustBulletsClass();
         FONT_BULLETS = "JustBulletsClass";
      }
      
      private function onAddedToStageHadler(param1:Event = null) : void
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
         this.initPool = [this.initStage,this.initKeyboard,this.initMouse,this.initCommunication,this.initEntities,this.initLocales];
         Locale.onSuccess = this.initSecond;
         setTimeout(this.initNext,0);
      }
      
      public function initSecond() : void
      {
         this.initPool = new Array();
         this.initPool = [this.initNavigator,this.initGUI,this.initSettings,this.initLogger];
         setTimeout(this.initNext,0);
      }
      
      private function initNext() : void
      {
         var _loc1_:* = null;
         if(this.initPool.length)
         {
            _loc1_ = this.initPool.shift();
            _loc1_();
            setTimeout(this.initNext,0);
         }
      }
      
      private function initLocales() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Base.initLocales");
         Locale.load(["KeyBinds","extendedGUI","DefaultModels","soOptionsGUI","BWPersonality","EULA"]);
         Locale.LoadKeybindsLocTable();
      }
      
      private function initSettings() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Base.initSettings");
         Settings.Update();
      }
      
      private function initMouse() : void
      {
         var loc1:* = undefined;
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
      
      protected function onMiddleMouseUp(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onMiddleMouseUp");
      }
      
      protected function onMiddleMouseDown(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onMiddleMouseDown");
      }
      
      protected function onRightMouseDown(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onRightMouseDown");
      }
      
      protected function onRightMouseUp(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onRightMouseUp");
      }
      
      protected function onDoubleClick(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onDoubleClick");
      }
      
      protected function onMouseMoveNow(param1:MouseEvent) : void
      {
      }
      
      protected function onMouseWheel(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onMouseWheel");
      }
      
      protected function onMiddleClick(param1:MouseEvent) : void
      {
         if(param1.target is TextField)
         {
            trace(param1.target.getTextFormat().font,param1.target.getTextFormat().bold);
         }
         Logger.LogToChannel(Logger.DEBUG,"onMiddleClick");
      }
      
      protected function onRightClickNow(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onRightClickNow");
      }
      
      protected function onSingleClick(param1:MouseEvent) : void
      {
      }
      
      protected function onSayMouseRotate(param1:MouseEvent) : *
      {
         var _loc2_:Array = new Array();
         if(Math.round(stage.mouseX - this.X) != 0)
         {
            _loc2_.push(stage.mouseX - this.X);
            Api.call("rotate_dummy",_loc2_);
         }
         this.X = stage.mouseX;
         this.Y = stage.mouseY;
      }
      
      protected function onSingleMouseDown(param1:MouseEvent) : void
      {
         this.X = stage.mouseX;
         this.Y = stage.mouseY;
         if(param1.target is Navigator)
         {
            Base.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onSayMouseRotate);
         }
      }
      
      protected function onSingleMouseUp(param1:MouseEvent) : void
      {
         Base.stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onSayMouseRotate);
      }
      
      private function initEntities() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Base.initEntities");
         Gold.Init();
         Character.Init();
         Premium.Init();
         ScreenMode.Init();
      }
      
      private function initCommunication() : void
      {
         var loc1:* = undefined;
         Logger.LogToChannel(Logger.DEBUG,"Base.initCommunication");
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
      
      private function externalInterfaceTransmit(param1:Object) : void
      {
         Api.onCallBack(param1);
      }
      
      protected function initKeyboard() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Base.initKeyboard");
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function initStage() : void
      {
         var _loc1_:* = undefined;
         Logger.LogToChannel(Logger.DEBUG,"Base.initStage");
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
      
      protected function onMouseMove(param1:MouseEvent) : void
      {
      }
      
      protected function onStageResize(param1:Event) : void
      {
         setTimeout(Base.stage.dispatchEvent,0,new Event(Base.STAGE_RESIZE));
         background.x = 0;
         background.y = 0;
         background.width = stage.stageWidth;
         background.height = stage.stageHeight;
      }
      
      protected function initLogger() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Base.initLogger");
         if(DEBUG)
         {
            Logger.init(Base.logLevel,true);
         }
         Logger.CustomChannels = [Logger.DEBUG,Logger.DEFAULT,Logger.ERROR,Logger.FATAL_ERROR,Logger.RX,Logger.TX,Logger.WARNING];
      }
      
      protected function onKeyDown(param1:KeyboardEvent) : void
      {
      }
      
      private function doneLoad(param1:Event) : void
      {
         this.myLoader.contentLoaderInfo.removeEventListener(Event.COMPLETE,this.doneLoad);
         background = Bitmap(this.myLoader.content);
         background.x = 0;
         background.y = 0;
         background.width = stage.stageWidth;
         background.height = stage.stageHeight;
         Base.stage.addChild(background);
      }
   }
}

