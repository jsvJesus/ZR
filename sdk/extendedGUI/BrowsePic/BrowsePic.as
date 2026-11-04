package
{
   import communication.Api;
   import communication.FileManager;
   import flash.display.Sprite;
   import flash.display.Stage;
   import flash.display.StageAlign;
   import flash.display.StageScaleMode;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.external.ExternalInterface;
   import flash.utils.setTimeout;
   import logging.Logger;
   import ui.Navigator;
   import ui.screens.FileManagerScreen;
   
   [SWF(frameRate="60",width="1024",height="768",backgroundColor="0x3995b0")]
   public class BrowsePic extends Sprite
   {
      public static var self:BrowsePic;
      
      public static var stage:Stage;
      
      public static var logLevel:Sprite;
      
      public static var comboLevel:Sprite;
      
      public static var contentLevel:Sprite;
      
      public static var CalloutLevel:Sprite;
      
      public static var navigator:Navigator;
      
      public static var ALIGN:String = StageAlign.TOP_LEFT;
      
      public static var SCALEMODE:String = StageScaleMode.NO_SCALE;
      
      public static const FILEMANAGER:String = "file_manager";
      
      public static var BG_SHIELD_IS_ACTIVE:Boolean = false;
      
      public static var BG_SHIELD_COLOR:int = 16711680;
      
      public static var BG_SHIELD_ALPHA:Number = 0.5;
      
      protected var shield:Sprite;
      
      protected var startInitDelay:uint = 200;
      
      protected var initDelay:uint = 50;
      
      protected var initPool:Array;
      
      public function BrowsePic()
      {
         super();
         BrowsePic.self = this;
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHadler);
      }
      
      protected function onAddedToStageHadler(event:Event) : void
      {
         BrowsePic.self = this;
         BrowsePic.stage = this.stage;
         Base.stage = this.stage;
         Logger.LogToChannel(Logger.DEBUG,"BrowsePic was added to stage");
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
         this.initPool = [this.initStage,this.initLogger,this.initKeyboard,this.initMouse,this.initCommunication,this.initFileManager,this.initLocales,this.initSecond];
         setTimeout(this.initNext,this.startInitDelay);
      }
      
      public function initSecond() : void
      {
         this.initPool = new Array();
         this.initPool = [this.initNavigator,this.initGUI,this.setReady];
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
         BrowsePic.self.stage.align = ALIGN;
         BrowsePic.self.stage.scaleMode = SCALEMODE;
         BrowsePic.self.stage.removeEventListener(Event.RESIZE,this.onStageResize);
         BrowsePic.self.stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         BrowsePic.self.stage.addEventListener(Event.RESIZE,this.onStageResize);
         BrowsePic.self.stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMouseMove);
         BrowsePic.self.stage.addChild(BrowsePic.contentLevel = new Sprite());
         BrowsePic.self.stage.addChild(BrowsePic.comboLevel = new Sprite());
         BrowsePic.self.stage.addChild(BrowsePic.CalloutLevel = new Sprite());
         BrowsePic.self.stage.addChild(BrowsePic.logLevel = new Sprite());
      }
      
      protected function onStageResize(event:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"stage resize:",BrowsePic.stage.stageWidth,BrowsePic.stage.stageHeight);
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
            this.shield.graphics.drawRect(0,0,BrowsePic.stage.stageWidth,BrowsePic.stage.stageHeight);
            this.shield.graphics.endFill();
         }
      }
      
      protected function initLogger() : void
      {
         Logger.init(BrowsePic.logLevel,false);
      }
      
      protected function initKeyboard() : void
      {
         BrowsePic.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
         BrowsePic.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
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
      }
      
      protected function initNavigator() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"initNavigator..");
         if(!navigator)
         {
            navigator = new Navigator(false,false,false);
            contentLevel.addChild(navigator);
         }
         Logger.LogToChannel(Logger.DEBUG,"initNavigator ok.");
      }
      
      protected function initGUI() : void
      {
         navigator.addScreen(new FileManagerScreen(FILEMANAGER));
         navigator.showScreen(FILEMANAGER);
      }
      
      protected function initFileManager() : void
      {
         FileManager.Init();
         Logger.LogToChannel(Logger.DEBUG,"initFileManager ok.");
      }
      
      protected function setReady() : void
      {
         Api.call(Api.READY);
         Logger.LogToChannel(Logger.DEBUG,"setReady ok.");
      }
   }
}

