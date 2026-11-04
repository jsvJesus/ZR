package
{
   import flash.display.Sprite;
   import flash.display.Stage;
   import flash.display.StageAlign;
   import flash.display.StageQuality;
   import flash.display.StageScaleMode;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.system.Capabilities;
   import flash.ui.Keyboard;
   import lang.Localization;
   import logging.Logger;
   import ui.ScreenFadeTransitionConductor;
   import ui.ScreenNavigator;
   import ui.features.parallax.ParallaxBackground;
   
   [SWF(frameRate="60",width="1024",height="768",backgroundColor="0x000000")]
   public class StalkerBase extends Sprite
   {
      public static var stage:Stage;
      
      public static const VERSION:String = "1.0.7";
      
      public static const RESET_KEY_CODE:uint = Keyboard.F11;
      
      public static var USE_FILTERS:Boolean = false;
      
      public static var QUALITY:String = StageQuality.MEDIUM;
      
      public static var ALIGN:String = StageAlign.TOP_LEFT;
      
      public static var SCALEMODE:String = StageScaleMode.NO_SCALE;
      
      public static var LOG_ALPHA:Number = 0.75;
      
      public static var LOG_VISIBLE_ON_START:Boolean = true;
      
      public static var BG_SHIELD_IS_ACTIVE:Boolean = true;
      
      public static var BG_SHIELD_COLOR:int = 0;
      
      public static var BG_SHIELD_ALPHA:Number = 1;
      
      protected var shield:Sprite;
      
      protected var parallaxBack:ParallaxBackground;
      
      private var _navigator:ScreenNavigator;
      
      public function StalkerBase()
      {
         super();
         Logger.Log("StalkerBase; version: ",VERSION,";",Capabilities.os,";",Capabilities.playerType,";",Capabilities.version);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
      }
      
      public static function get isScaleform() : Boolean
      {
         return String(Capabilities.version).toLowerCase().indexOf("win") < 0;
      }
      
      public function get navigator() : ScreenNavigator
      {
         return this._navigator;
      }
      
      public function set navigator(value:ScreenNavigator) : void
      {
         this._navigator = value;
      }
      
      private function onAddedToStageHadler(event:Event = null) : void
      {
         StalkerBase.stage = this.stage;
         Logger.LogToChannel(Logger.DEBUG,"added to stage");
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
         this.reset();
      }
      
      public function fail(... args) : void
      {
         trace("fail",args);
      }
      
      public function success(... args) : void
      {
         trace("success",args);
         this.reset();
      }
      
      public function reset() : void
      {
         Localization.init("GUI.xml");
         this.initKeyboard();
         this.initStage();
         this.initAppBackground();
         this.initParallaxBackground();
         this.initPerspectiveFollower();
         this.initLogger();
         this.initNavigator();
         this.test();
      }
      
      protected function initKeyboard() : void
      {
         this.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         this.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
         trace(event.keyCode,event.keyCode == RESET_KEY_CODE);
         if(event.keyCode == RESET_KEY_CODE)
         {
            this.reset();
         }
      }
      
      protected function initNavigator() : void
      {
         if(Boolean(this.navigator))
         {
            this.removeChild(this.navigator);
            this.navigator = null;
         }
         this.navigator = new ScreenNavigator();
         new ScreenFadeTransitionConductor(this.navigator);
         this.addChild(this.navigator);
         this.addScreens();
      }
      
      protected function addScreens() : void
      {
         trace("addScreens");
      }
      
      protected function initStage() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"init stage..");
         this.stage.align = ALIGN;
         this.stage.scaleMode = SCALEMODE;
         Logger.LogToChannel(Logger.DEBUG,"QUALITY: ",QUALITY,"; ALIGN: ",ALIGN,"; SCALEMODE:",SCALEMODE,";");
         this.stage.removeEventListener(Event.RESIZE,this.onStageResize);
         this.stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         this.stage.addEventListener(Event.RESIZE,this.onStageResize);
         this.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
      }
      
      protected function initAppBackground() : void
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
      
      protected function initPerspectiveFollower() : void
      {
      }
      
      protected function initParallaxBackground() : void
      {
         if(Boolean(this.parallaxBack))
         {
            this.parallaxBack.destroy();
            this.removeChild(this.parallaxBack);
            this.parallaxBack = null;
         }
         this.parallaxBack = new ParallaxBackground();
         this.addChild(this.parallaxBack);
      }
      
      protected function initLogger() : void
      {
         Logger.init(this,true);
      }
      
      protected function test() : void
      {
      }
      
      protected function onMouseMove(event:MouseEvent) : void
      {
      }
      
      protected function onStageResize(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"stage resize:",this.stage.stageWidth,this.stage.stageHeight);
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
      
      protected function logSystemCapabilities() : void
      {
      }
      
      protected function logSystemCapabilitiesShort() : void
      {
         Logger.Log(Capabilities.os,":\t - current OS");
         Logger.Log(Capabilities.playerType,":\t - type of runtime environment");
         Logger.Log(Capabilities.version,":\t - specifies player/platform version");
      }
   }
}

