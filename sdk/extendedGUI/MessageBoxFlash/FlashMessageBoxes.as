package
{
   import com.MessageConstans;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.external.ExternalInterface;
   import flash.utils.setTimeout;
   import scaleform.clik.core.UIComponent;
   
   public class FlashMessageBoxes extends UIComponent
   {
      protected var OK:Function = new Function();
      
      protected var Cancel:Function = new Function();
      
      protected var MessageBoxes:Object = new Object();
      
      protected var Local:Object = new Object();
      
      protected var _tmpArray:Array = new Array();
      
      protected var tt:Number = 0;
      
      internal var messageBoard:Sprite = new Sprite();
      
      protected var _focus:Boolean = false;
      
      public function FlashMessageBoxes()
      {
         super();
         ExternalInterface.addCallback("show_message",this.showMsg);
         ExternalInterface.addCallback("send_hide_message",this.hideMsg);
         ExternalInterface.addCallback("send_hide_all_messages",this.hideAllMsg);
         ExternalInterface.addCallback("validate_input",this.setInput);
         ExternalInterface.addCallback("localized_resource",this.setLocaleFunc);
         stage.addEventListener(MouseEvent.CLICK,this.onMouseClick);
         addChild(this.messageBoard);
         this.messageBoard.visible = false;
         stage.addEventListener(Event.RESIZE,this.onRes);
         setTimeout(this.getLocaleFunc,100);
      }
      
      public function getLocaleFunc() : *
      {
         ExternalInterface.call("localized_resource",{"paths":["FlashMessageBoxes"]});
      }
      
      public function setLocaleFunc(S:*) : *
      {
         MessageConstans.Local = S.FlashMessageBoxes;
         this.showTmp();
      }
      
      public function showTmp() : *
      {
         for(var i:int = 0; i < this._tmpArray.length; i++)
         {
            this.showMsg(this._tmpArray.pop());
         }
      }
      
      public function showMessage(name:String, messageTitle:String, messageText:String, input:Boolean, lock:Boolean, btns:Array, btnNames:Object, def:String, active:Boolean) : *
      {
         if(this.MessageBoxes[name] != null)
         {
            this.MessageBoxes[name].destroy();
            this.MessageBoxes[name] = null;
         }
         this.MessageBoxes[name] = new MessageBox();
         addChild(this.MessageBoxes[name]);
         this.MessageBoxes[name].setParam(name,messageTitle,messageText,input,btns,btnNames,def,active);
         this.messageBoard.graphics.beginFill(0,0.2);
         this.messageBoard.graphics.drawRect((Object(root).width - stage.stageWidth) / 2,(Object(root).height - stage.stageHeight) / 2,(stage.stageWidth + Object(root).width) / 2,(Object(root).height + stage.stageHeight) / 2);
         this.messageBoard.graphics.endFill();
         this.messageBoard.visible = lock;
         this.MessageBoxes[name].x = ((Object(root).width - stage.stageWidth) / 2 + stage.stageWidth) / 2 - this.MessageBoxes[name].width / 2;
         this.MessageBoxes[name].y = ((Object(root).height - stage.stageHeight) / 2 + stage.stageHeight) / 2 - this.MessageBoxes[name].height / 2;
         this.MessageBoxes[name].show();
      }
      
      public function onRes(e:Event) : *
      {
         var i:* = undefined;
         this.messageBoard.x = (Object(root).width - stage.stageWidth) / 2;
         this.messageBoard.y = (Object(root).height - stage.stageHeight) / 2;
         this.messageBoard.width = stage.stageWidth + stage.stageWidth / 2;
         this.messageBoard.height = stage.stageHeight + stage.stageHeight / 2;
         for(i in this.MessageBoxes)
         {
            this.MessageBoxes[i].x = ((Object(root).width - stage.stageWidth) / 2 + stage.stageWidth) / 2 - this.MessageBoxes[i].width / 2;
            this.MessageBoxes[i].y = ((Object(root).width - stage.stageWidth) / 2 + stage.stageWidth) / 2 - this.MessageBoxes[i].width / 2;
         }
      }
      
      public function set mouseFocus(value:Boolean) : *
      {
         if(value == this._focus)
         {
            return;
         }
      }
      
      public function showMsg(arg:Object) : *
      {
         if(!arg.name)
         {
            this.errorToPython(MessageConstans.ERROR_NAME);
            return;
         }
         if(MessageConstans.Local == null)
         {
            this._tmpArray.push(arg);
            return;
         }
         var _name:String = arg.name;
         var messageTitle:String = "";
         var messageText:String = "";
         var _lock:Boolean = false;
         var _input:Boolean = false;
         var _btns:Array = [MessageConstans.BTN_OK];
         var _names:Object = new Object();
         var _default:String = "";
         var _active:Boolean = false;
         if(arg.title)
         {
            messageTitle = arg.title;
         }
         if(arg.text)
         {
            messageText = arg.text;
         }
         if(arg.lock)
         {
            _lock = Boolean(arg.lock);
         }
         if(arg.input)
         {
            _input = Boolean(arg.input);
         }
         if(arg.buttons)
         {
            _btns = arg.buttons;
         }
         if(arg.names)
         {
            _names = arg.names;
         }
         if(arg.§default§)
         {
            _default = arg.§default§;
         }
         if(arg.active)
         {
            _active = Boolean(arg.active);
         }
         this.showMessage(_name,messageTitle,messageText,_input,_lock,_btns,_names,_default,_active);
      }
      
      public function hideMsg(arg:String) : *
      {
         if(this.MessageBoxes[arg] != null)
         {
            this.MessageBoxes[arg].destroy();
            this.MessageBoxes[arg] = null;
         }
      }
      
      public function hideAllMsg(arg:*) : *
      {
         var i:* = undefined;
         for(i in this.MessageBoxes)
         {
            this.MessageBoxes[i].destroy();
            this.MessageBoxes[i] = null;
         }
      }
      
      private function errorToPython(text:String = " ") : *
      {
         var obj:Object = new Object();
         obj.text = text;
         ExternalInterface.call("showError",obj);
      }
      
      private function setInput(arg:Object) : *
      {
         if(!arg.name)
         {
            this.errorToPython("ERROR");
            return;
         }
         if(this.MessageBoxes[arg.name] != null)
         {
            this.MessageBoxes[arg.name].validateInput(arg.result);
         }
      }
      
      protected function onMouseClick(e:MouseEvent) : *
      {
         var obj:Object = new Object();
         if(e.eventPhase == 2)
         {
            stage.focus = null;
         }
      }
   }
}

