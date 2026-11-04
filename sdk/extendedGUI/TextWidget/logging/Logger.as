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
      
      public static function set self(param1:Logger) : void
      {
         _self = param1;
      }
      
      private static function get log() : Vector.<LogObject>
      {
         if(!_log)
         {
            _log = new Vector.<LogObject>();
         }
         return _log;
      }
      
      private static function set log(param1:Vector.<LogObject>) : void
      {
         _log = param1;
      }
      
      public static function init(param1:DisplayObjectContainer, param2:Boolean = false) : void
      {
         if(base == param1)
         {
            return;
         }
         initBase(param1);
         initKeyboardWorks(base);
         initTextField(param2);
         if(param2)
         {
            addSelfToBase();
         }
      }
      
      public static function Hide() : void
      {
         removeSelfFromBase();
      }
      
      public static function Re(param1:Object, param2:String = ">\t\t") : void
      {
         var _loc3_:* = null;
         var _loc4_:* = 0;
         var _loc5_:* = param1;
         for(_loc3_ in _loc5_)
         {
            Logger.LogToChannel(Logger.DEBUG,param2,_loc3_,":",param1[_loc3_]);
            Re(param1[_loc3_],param2 + "\t");
         }
      }
      
      private static function initKeyboardWorks(param1:DisplayObjectContainer) : void
      {
         if(param1.stage != null)
         {
            param1.stage.addEventListener(KeyboardEvent.KEY_DOWN,onKeyDown);
         }
      }
      
      protected static function onKeyDown(param1:KeyboardEvent) : void
      {
         if(param1.keyCode == SWITCH_KEY_CODE)
         {
            doSwitchLogger();
         }
         var _loc2_:* = param1.keyCode;
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
      
      private static function doSwitchLogger(param1:Boolean = false) : void
      {
         trace("doSwitchLogger",SWITCHED_ON);
         SWITCHED_ON = !SWITCHED_ON;
         if(Base.logLevel)
         {
            Base.logLevel.visible = SWITCHED_ON;
         }
      }
      
      private static function onSwitchCompleted() : void
      {
         trace("SWITCHED_ON",SWITCHED_ON);
         self.visible = SWITCHED_ON;
         if(SWITCHED_ON && DEFAULT_ROTATION_Y == 0)
         {
         }
      }
      
      private static function fix3DBlur(param1:DisplayObject) : void
      {
         param1.scaleX = param1.width / (param1.width - 1);
         param1.scaleY = param1.height / (param1.height - 1);
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
      
      private static function initBase(param1:DisplayObjectContainer) : void
      {
         base = param1;
         base.stage.addEventListener(Event.ADDED,onSomeToStageAdded);
      }
      
      protected static function onSomeToStageAdded(param1:Event) : void
      {
         trace("========================================",param1.target);
         bringMeToFront(param1.target);
      }
      
      private static function bringMeToFront(param1:*) : void
      {
         if(self.parent)
         {
            self.parent.setChildIndex(self,self.parent.numChildren - 1);
         }
      }
      
      private static function initTextField(param1:Boolean = false) : void
      {
         bg = new Shape();
         self.addChild(bg);
         tf = new TextField();
         tf.width = 600;
         tf.autoSize = TextFieldAutoSize.LEFT;
         var _loc2_:* = new TextFormat("Arial",12,16777215,true);
         _loc2_.tabStops = [100,200];
         tf.defaultTextFormat = _loc2_;
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
         if(!param1)
         {
            doSwitchLogger(true);
         }
         self.addChild(tf);
         printLog();
      }
      
      public static function LogToChannel(param1:String = "Def", ... rest) : void
      {
         var _loc3_:* = new LogObject(rest,param1);
         log.push(_loc3_);
         printLog();
         traceLogObject(_loc3_);
      }
      
      private static function traceLogObject(param1:LogObject) : void
      {
         trace(param1.timer + "\t[" + param1.channel + "]:\t" + param1.toString());
      }
      
      public static function clear(... rest) : void
      {
         log = new Vector.<LogObject>();
         printLog();
      }
      
      private static function getFontByChannel(param1:String) : String
      {
         var _loc2_:* = param1;
      }
      
      public static function get channels() : Array
      {
         if(Boolean(CustomChannels) && Boolean(CustomChannels.length))
         {
            return CustomChannels;
         }
         return defaultChannels;
      }
      
      private static function linkClicked(param1:TextEvent) : void
      {
         Logger.LogToChannel(Logger.DEFAULT,param1.text);
      }
      
      public static function get CustomChannels() : Array
      {
         return _customChannels;
      }
      
      public static function set CustomChannels(param1:Array) : void
      {
         _customChannels = param1;
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
      
      public static function set regPoint(param1:Point) : void
      {
         _regPoint = param1;
         printLog();
      }
      
      private static function printLog() : void
      {
         var _loc1_:* = null;
         var _loc2_:* = null;
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
         var _loc3_:* = new String();
         var _loc4_:* = START_LINE >= 0 ? START_LINE : Math.max(log.length - MAX_LINES,0);
         var _loc5_:* = _loc4_ + MAX_LINES;
         var _loc6_:* = _loc4_;
         while(_loc6_ < log.length)
         {
            if(channels.indexOf(log[_loc6_].channel) >= 0)
            {
               if(SHOW_CHANNELS)
               {
                  _loc1_ = log[_loc6_].toString();
                  if(_loc1_.length > MAX_SYMBOLS_PER_BIG_LINE && log[_loc6_].channel != DEFAULT)
                  {
                     _loc1_ = _loc1_.substr(0,Math.min(MAX_SYMBOLS_PER_BIG_LINE,_loc1_.length - 1)) + " ... <a href=\'event:" + _loc1_ + "\'>" + EXTEND_FONT + "[more]" + "</font>" + "</a>";
                  }
                  _loc1_ = getFontByChannel(log[_loc6_].channel) + _loc1_ + "</font>";
                  _loc2_ = getFontByChannel(log[_loc6_].channel) + _loc6_ + " " + log[_loc6_].timer + " " + "<b>" + "[" + log[_loc6_].channel + "]:" + "</b></font>";
                  _loc3_ += _loc2_ + _loc1_ + SPLITTER;
               }
               else
               {
                  _loc3_ += log[_loc6_] + SPLITTER;
               }
            }
            _loc6_++;
         }
         if(tf)
         {
            tf.htmlText = _loc3_;
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
         var _loc2_:* = new LogObject(rest);
         log.push(_loc2_);
         printLog();
         traceLogObject(_loc2_);
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

