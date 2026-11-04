package logging
{
   import flash.display.*;
   import flash.events.*;
   import flash.filters.*;
   import flash.geom.*;
   import flash.text.*;
   import flash.ui.*;
   
   public class Logger extends Sprite
   {
      private static var _customChannels:Array;
      
      private static var tf:TextField;
      
      private static var bg:Shape;
      
      private static var _self:Logger;
      
      private static var _regPoint:Point;
      
      private static var _log:Vector.<LogObject>;
      
      private static var base:DisplayObjectContainer;
      
      public static const DEFAULT_FONT:String = "<font color=\'#ffffff\' size=\'12\' face=\'Arial\'>";
      
      public static const ERROR_FONT:String = "<font color=\'#ed3340\' size=\'12\' face=\'Arial\'>";
      
      public static const FATAL_ERROR_FONT:String = "<font color=\'#b9000d\' size=\'12\' face=\'Arial\'>";
      
      public static const SPLITTER:String = "\n";
      
      public static const LOCALIZATION_FONT:String = "<font color=\'#b2b2b2\' size=\'12\' face=\'Arial\'>";
      
      public static const SWITCH_KEY_CODE:uint = Keyboard.G;
      
      public static const defaultChannels:Array = [DEFAULT,WARNING,ERROR,DEBUG,LOCALIZATION,TX,RX];
      
      private static const DEFAULT_ROTATION_Y:int = 0;
      
      public static const DEFAULT:String = "Def";
      
      public static const DEBUG:String = "Dbg";
      
      public static const TX:String = "Tx»";
      
      public static const RX:String = "Rx«";
      
      public static const WARNING:String = "Wrn";
      
      public static const ERROR:String = "Err";
      
      public static const FATAL_ERROR:String = "Fatal error";
      
      public static const LOCALIZATION:String = "Lcl";
      
      public static const EXTEND_FONT:String = "<font color=\'#e8e49f\' size=\'12\' face=\'Arial\'>";
      
      private static const HIDED_ROTATION_Y:int = -90;
      
      public static const DEBUG_FONT:String = "<font color=\'#00fcff\' size=\'12\' face=\'Arial\'>";
      
      public static const TX_FONT:String = "<font color=\'#70a0c9\' size=\'12\' face=\'Arial\'>";
      
      public static const RX_FONT:String = "<font color=\'#70c97d\' size=\'12\' face=\'Arial\'>";
      
      public static const WARNING_FONT:String = "<font color=\'#e8ea23\' size=\'12\' face=\'Arial\'>";
      
      public static var MAX_LINES:uint = 20;
      
      public static var MAX_SYMBOLS_PER_BIG_LINE:uint = 180;
      
      public static var SHOW_CHANNELS:Boolean = true;
      
      public static var SWITCHED_ON:Boolean = true;
      
      public static var use3d:Boolean = false;
      
      public static var START_LINE:int = -1;
      
      START_LINE = -1;
      MAX_LINES = 20;
      MAX_SYMBOLS_PER_BIG_LINE = 180;
      SHOW_CHANNELS = true;
      SWITCHED_ON = true;
      use3d = false;
      
      public function Logger()
      {
         super();
      }
      
      public static function set self(arg1:Logger) : void
      {
         _self = arg1;
      }
      
      private static function get log() : Vector.<LogObject>
      {
         if(!_log)
         {
            _log = new Vector.<LogObject>();
         }
         return _log;
      }
      
      private static function set log(arg1:Vector.<LogObject>) : void
      {
         _log = arg1;
      }
      
      public static function init(arg1:DisplayObjectContainer, arg2:Boolean = false) : void
      {
         if(base == arg1)
         {
            return;
         }
         initBase(arg1);
         initKeyboardWorks(base);
         initTextField(arg2);
         if(arg2)
         {
            addSelfToBase();
         }
      }
      
      public static function Hide() : void
      {
         removeSelfFromBase();
      }
      
      public static function Re(arg1:Object, arg2:String = ">\t\t") : void
      {
         var loc1:* = null;
         var loc2:* = 0;
         var loc3:* = arg1;
         for(loc1 in loc3)
         {
            Logger.LogToChannel(Logger.DEBUG,arg2,loc1,":",arg1[loc1]);
            Re(arg1[loc1],arg2 + "\t");
         }
      }
      
      private static function initKeyboardWorks(arg1:DisplayObjectContainer) : void
      {
         if(arg1.stage != null)
         {
            arg1.stage.addEventListener(KeyboardEvent.KEY_DOWN,onKeyDown);
         }
      }
      
      protected static function onKeyDown(arg1:KeyboardEvent) : void
      {
         if(arg1.keyCode == SWITCH_KEY_CODE)
         {
            doSwitchLogger();
         }
         var loc1:* = arg1.keyCode;
      }
      
      public static function Show() : void
      {
         addSelfToBase();
      }
      
      public static function ScrollUp() : void
      {
         if(START_LINE < 0)
         {
            START_LINE = log.length - MAX_LINES - 1;
         }
         else
         {
            --START_LINE;
         }
         if(START_LINE < 0)
         {
            START_LINE = 0;
         }
         printLog();
      }
      
      public static function ScrollDown() : void
      {
         if(START_LINE < 0)
         {
            START_LINE = log.length - MAX_LINES - 1;
         }
         else
         {
            START_LINE += 1;
         }
         if(START_LINE > log.length - MAX_LINES)
         {
            START_LINE = log.length - MAX_LINES;
         }
         printLog();
      }
      
      public static function ScrollHome() : void
      {
         START_LINE = 0;
         printLog();
      }
      
      public static function ScrollEnd() : void
      {
         START_LINE = -1;
         printLog();
      }
      
      public static function Switch() : void
      {
         doSwitchLogger();
      }
      
      private static function doSwitchLogger(arg1:Boolean = false) : void
      {
         SWITCHED_ON = !SWITCHED_ON;
         if(Base.logLevel)
         {
            Base.logLevel.visible = SWITCHED_ON;
         }
      }
      
      private static function onSwitchCompleted() : void
      {
         self.visible = SWITCHED_ON;
         if(SWITCHED_ON && DEFAULT_ROTATION_Y == 0)
         {
         }
      }
      
      private static function fix3DBlur(arg1:DisplayObject) : void
      {
         arg1.scaleX = arg1.width / (arg1.width - 1);
         arg1.scaleY = arg1.height / (arg1.height - 1);
      }
      
      private static function addSelfToBase() : void
      {
         (base as DisplayObjectContainer).addChild(self);
      }
      
      private static function removeSelfFromBase() : void
      {
         if((base as DisplayObjectContainer).contains(self))
         {
            (base as DisplayObjectContainer).removeChild(self);
         }
      }
      
      private static function initBase(arg1:DisplayObjectContainer) : void
      {
         base = arg1;
         base.stage.addEventListener(Event.ADDED,onSomeToStageAdded);
      }
      
      protected static function onSomeToStageAdded(arg1:Event) : void
      {
         bringMeToFront(arg1.target);
      }
      
      private static function bringMeToFront(arg1:*) : void
      {
         if(self.parent)
         {
            self.parent.setChildIndex(self,self.parent.numChildren - 1);
         }
      }
      
      private static function initTextField(arg1:Boolean = false) : void
      {
         bg = new Shape();
         self.addChild(bg);
         tf = new TextField();
         tf.width = 600;
         tf.autoSize = TextFieldAutoSize.LEFT;
         var loc1:* = new TextFormat("Arial",12,16777215,true);
         loc1.tabStops = [100,200];
         tf.defaultTextFormat = loc1;
         tf.selectable = false;
         tf.wordWrap = true;
         tf.borderColor = 13421772;
         tf.border = true;
         tf.addEventListener(TextEvent.LINK,linkClicked);
         if(Base.USE_FILTERS)
         {
            tf.filters = [new GlowFilter(0,1,4,4,3,1)];
         }
         if(use3d)
         {
            tf.rotationY = DEFAULT_ROTATION_Y;
         }
         if(!arg1)
         {
            doSwitchLogger(true);
         }
         self.addChild(tf);
         printLog();
      }
      
      public static function LogToChannel(arg1:String = "Def", ... rest) : void
      {
         var loc1:* = new LogObject(rest,arg1);
         log.push(loc1);
         printLog();
         traceLogObject(loc1);
      }
      
      private static function traceLogObject(arg1:LogObject) : void
      {
      }
      
      public static function clear(... rest) : void
      {
         log = new Vector.<LogObject>();
         printLog();
      }
      
      private static function getFontByChannel(arg1:String) : String
      {
         var loc1:* = arg1;
      }
      
      public static function get channels() : Array
      {
         if(Boolean(CustomChannels) && Boolean(CustomChannels.length))
         {
            return CustomChannels;
         }
         return defaultChannels;
      }
      
      private static function linkClicked(arg1:TextEvent) : void
      {
         Logger.LogToChannel(Logger.DEFAULT,arg1.text);
      }
      
      public static function get CustomChannels() : Array
      {
         return _customChannels;
      }
      
      public static function set CustomChannels(arg1:Array) : void
      {
         _customChannels = arg1;
         printLog();
      }
      
      public static function ShowChannels(... rest) : void
      {
         if(!rest.length)
         {
            return;
         }
         if(!_customChannels)
         {
            _customChannels = new Array();
         }
         _customChannels = _customChannels.concat(rest);
         printLog();
      }
      
      public static function ResetChannels() : void
      {
         _customChannels = null;
         printLog();
      }
      
      public static function set regPoint(arg1:Point) : void
      {
         _regPoint = arg1;
         printLog();
      }
      
      private static function printLog() : void
      {
         var loc2:* = null;
         var loc3:* = null;
         if(Boolean(self) && Boolean(self.stage))
         {
            if(_regPoint == null)
            {
               self.x = self.stage.stageWidth * 0.5;
               self.y = 47;
            }
            else
            {
               self.x = _regPoint.x;
               self.y = _regPoint.y;
            }
         }
         var loc1:* = new String();
         var loc4:* = START_LINE >= 0 ? START_LINE : Math.max(log.length - MAX_LINES,0);
         var loc5:* = loc4 + MAX_LINES;
         var loc6:* = loc4;
         while(loc6 < log.length)
         {
            if(channels.indexOf(log[loc6].channel) >= 0)
            {
               if(SHOW_CHANNELS)
               {
                  loc2 = log[loc6].toString();
                  if(loc2.length > MAX_SYMBOLS_PER_BIG_LINE && log[loc6].channel != DEFAULT)
                  {
                     loc2 = loc2.substr(0,Math.min(MAX_SYMBOLS_PER_BIG_LINE,loc2.length - 1)) + " ... <a href=\'event:" + loc2 + "\'>" + EXTEND_FONT + "[more]" + "</font>" + "</a>";
                  }
                  loc2 = getFontByChannel(log[loc6].channel) + loc2 + "</font>";
                  loc3 = getFontByChannel(log[loc6].channel) + loc6 + " " + log[loc6].timer + " " + "<b>" + "[" + log[loc6].channel + "]:" + "</b></font>";
                  loc1 += loc3 + loc2 + SPLITTER;
               }
               else
               {
                  loc1 += log[loc6] + SPLITTER;
               }
            }
            loc6++;
         }
         if(tf)
         {
            tf.htmlText = loc1;
            if(self.stage)
            {
               if(_regPoint == null)
               {
                  tf.width = self.stage.stageWidth * 0.5;
                  tf.height = self.stage.stageHeight - 47;
               }
               else
               {
                  tf.width = self.stage.stageWidth - _regPoint.x;
                  tf.height = self.stage.stageHeight - _regPoint.y;
               }
               bg.graphics.clear();
               bg.graphics.beginFill(0,0.7);
               bg.graphics.drawRect(0,0,tf.width,tf.height);
               bg.graphics.endFill();
            }
         }
      }
      
      public static function Log(... rest) : void
      {
         var loc1:* = new LogObject(rest);
         log.push(loc1);
         printLog();
         traceLogObject(loc1);
      }
      
      public static function get self() : Logger
      {
         if(!_self)
         {
            _self = new Logger();
         }
         return _self;
      }
   }
}

