package ui.components
{
   import com.dvalimona.components.*;
   import communication.*;
   import flash.events.*;
   import flash.text.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.*;
   
   public class ConfirmWindow extends Window
   {
      public static var self:ConfirmWindow;
      
      public static const EMAIL:String = "email";
      
      public static const SMS:String = "sms";
      
      protected var count:uint;
      
      protected var deliveredBy:String;
      
      protected var msg:String;
      
      protected var _time:Number;
      
      protected var vBox:VBox;
      
      protected var otherItems:HBox;
      
      protected var description:Text;
      
      protected var enterLine:HBoxLine;
      
      protected var enterLabel:LabelShadowed;
      
      protected var enterField:InputText;
      
      protected var enterButton:PushButton;
      
      protected var tryCountLabel:LabelShadowed;
      
      protected var quad:Quad;
      
      protected var refreshLabel:Text;
      
      protected var changeSendModeLabel:Text;
      
      protected var refreshButton:PushButton;
      
      protected var exitButton:PushButton;
      
      protected var changeSendModeButton:ClearButton;
      
      protected var timer:Timer = new Timer(1000);
      
      protected var allButtons:Array;
      
      public function ConfirmWindow(count:uint, deliveredBy:String, msg:String, time:Number)
      {
         this.count = count;
         this.deliveredBy = deliveredBy;
         this.msg = msg;
         this.time = getTimer() / 1000 + time;
         super();
         this.timer.addEventListener(TimerEvent.TIMER,this.timerTick);
      }
      
      public static function getInstance(count:uint, deliveredBy:String, msg:String, time:Number) : ConfirmWindow
      {
         if(!self)
         {
            self = new ConfirmWindow(count,deliveredBy,msg,time);
         }
         else
         {
            self.count = count;
            self.deliveredBy = deliveredBy;
            self.msg = msg;
            self.time = getTimer() / 1000 + time;
            self.enterField.text = "";
            self.enterField.enabled = true;
            self.enterButton.enabled = false;
         }
         self.invalidate();
         return self;
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.headerDeals();
         this.bodyDeals();
      }
      
      public function set time(value:Number) : void
      {
         _timer = value;
         this.timer.start();
      }
      
      public function get time() : Number
      {
         return _timer;
      }
      
      override protected function showOn() : void
      {
      }
      
      private function bodyDeals() : void
      {
         this.vBox = new VBox();
         this.vBox.x = 0;
         this.vBox.y = 0;
         this.vBox.alignment = VBox.JUSTIFY;
         this.vBox.spacing = 10;
         this.vBox.debug = false;
         this.addChild(this.vBox);
         this.description = new Text(this.vBox);
         this.description.size = 26;
         this.description.debug = false;
         this.description.editable = false;
         this.description.paddingTop = 20;
         this.description.paddingLeft = 20;
         this.description.paddingRight = 20;
         this.description.paddingBottom = 20;
         this.enterLine = new HBoxLine(this.vBox);
         this.enterLine.height = 50;
         this.enterLine.paddingTop = 0;
         this.enterLine.paddingLeft = 20;
         this.enterLine.paddingRight = 20;
         this.enterLine.paddingBottom = 0;
         this.enterLabel = new LabelShadowed(this.enterLine.left);
         this.enterLabel.size = 22;
         this.enterLabel.debug = false;
         this.enterButton = new PushButton(this.enterLine.right);
         this.enterButton.setSize(50,40);
         this.enterButton.addEventListener(MouseEvent.CLICK,this.onSendClick);
         this.enterButton.enabled = false;
         this.enterField = new InputText(this.enterLine.right);
         this.enterField.setSize(300,40);
         this.enterField.size = 22;
         this.enterField.addEventListener(Event.CHANGE,this.onFieldChanged);
         this.enterField.restrict = "A-Z 0-9 a-z _";
         this.tryCountLabel = new LabelShadowed(this.vBox);
         this.tryCountLabel.size = 18;
         this.tryCountLabel.color = 11776934;
         this.tryCountLabel.debug = false;
         this.tryCountLabel.paddingTop = 0;
         this.tryCountLabel.paddingLeft = 20;
         this.tryCountLabel.paddingRight = 20;
         this.tryCountLabel.paddingBottom = 0;
         this.quad = new Quad(this.vBox);
         this.quad.paddingTop = 20;
         this.quad.paddingLeft = 20;
         this.quad.paddingRight = 20;
         this.quad.paddingBottom = 20;
         this.refreshLabel = new Text(this.vBox);
         this.refreshLabel.size = 26;
         this.refreshLabel.paddingTop = 0;
         this.refreshLabel.paddingLeft = 20;
         this.refreshLabel.paddingRight = 20;
         this.refreshLabel.paddingBottom = 0;
         this.refreshLabel.editable = false;
         this.otherItems = new HBox();
         this.otherItems.alignment = HBox.LEFT;
         this.otherItems.fixedHeight = headerHeight;
         this.otherItems.horizontalAlign = HBox.LEFT;
         this.vBox.addChild(this.otherItems);
         this.changeSendModeLabel = new Text(this.otherItems);
         this.changeSendModeLabel.size = 26;
         this.changeSendModeLabel.paddingTop = 0;
         this.changeSendModeLabel.paddingLeft = 20;
         this.changeSendModeLabel.paddingRight = 0;
         this.changeSendModeLabel.paddingBottom = 0;
         this.changeSendModeLabel.editable = false;
         this.changeSendModeLabel.width = 450;
         this.changeSendModeLabel.textField.autoSize = TextFieldAutoSize.LEFT;
         this.changeSendModeButton = new ClearButton();
         this.changeSendModeButton.$ = "сюда";
         this.changeSendModeButton.addEventListener(MouseEvent.CLICK,this.onChangeSendModeHandler);
         this.changeSendModeButton.setSize(200,30);
         this.changeSendModeButton.size = 26;
         this.changeSendModeButton.y += 4;
         this.changeSendModeButton.paddingTop = 4;
         this.changeSendModeButton.underline = false;
         this.changeSendModeButton.autoWidth = true;
         this.changeSendModeButton.tabEnabled = true;
         this.otherItems.addChild(this.changeSendModeButton);
         this.refreshButton = new PushButton(this.vBox);
         this.refreshButton.paddingTop = 20;
         this.refreshButton.paddingBottom = 0;
         this.refreshButton.height = 50;
         this.refreshButton.addEventListener(MouseEvent.CLICK,this.onRefreshClick);
      }
      
      protected function onRefreshClick(event:MouseEvent) : void
      {
         this.refreshButton.enabled = false;
         Api.call(Api.CONFIRM_WINDOW_RESEND);
      }
      
      protected function onSendClick(event:MouseEvent) : void
      {
         this.enterButton.enabled = false;
         this.enterField.enabled = false;
         Api.call(Api.CONFIRM_WINDOW_OK,[{"key":this.enterField.text}]);
      }
      
      protected function onChangeSendModeHandler(event:MouseEvent) : void
      {
         Navigator.showOff([this,_titleBar,_titleLabel,_panel,this.description,this.enterLine,this.tryCountLabel,this.refreshLabel,this.refreshButton],this.psevdo_close_complete);
      }
      
      protected function psevdo_close_complete() : *
      {
         Api.call(Api.CONFIRM_WINDOW_CHANGE_TYPE);
         this.onShowOffComplete();
      }
      
      protected function onFieldChanged(event:Event) : void
      {
         this.enterButton.enabled = this.enterField.text.length > 0;
      }
      
      private function headerDeals() : void
      {
         leftItems.shift = 3;
         _titleLabel.autoSize = true;
         _titleLabel.font = Base.boldFontName;
         _titleLabel.text = "Stalker Online GUARD";
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
         this.exitButton = new ClearButton(this.rightItems);
         this.exitButton.autoWidth = true;
         this.exitButton.label = "выход";
         this.exitButton.addEventListener(MouseEvent.CLICK,this.onExitClick);
      }
      
      protected function onExitClick(event:MouseEvent) : void
      {
         Base.navigator.hideShields();
         this.doClose();
         Auth.self.doLogout();
      }
      
      public function doClose() : void
      {
         this.timer.stop();
         dispatchEvent(new Event(Event.CLOSE));
         this.close();
      }
      
      override protected function close() : void
      {
         Navigator.showOff([this,_titleBar,_titleLabel,_panel,this.description,this.enterLine,this.tryCountLabel,this.refreshLabel,this.refreshButton],this.onShowOffComplete);
      }
      
      private function onShowOffComplete() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onShowOffComplete");
         if(this.parent != null)
         {
            setTimeout(this.parent.removeChild,0,this);
         }
         setTimeout(this.killMePlease,10,this);
      }
      
      private function killMePlease() : void
      {
         self = null;
      }
      
      override public function draw() : void
      {
         super.draw();
         this.vBox.setSize(this.width,1);
         var code_type:String = "";
         switch(this.deliveredBy)
         {
            case EMAIL:
               this.description.text = Locale.getById("extendedGUI.ConfirmWindow.onEmail1") + " " + this.msg + " " + Locale.getById("extendedGUI.ConfirmWindow.onEmail2");
               code_type = SMS;
               break;
            case SMS:
            default:
               this.description.text = Locale.getById("extendedGUI.ConfirmWindow.onPhone1") + " " + this.msg + " " + Locale.getById("extendedGUI.ConfirmWindow.onPhone2");
               code_type = EMAIL;
         }
         this.enterLabel.text = Locale.getById("extendedGUI.ConfirmWindow.enterCode");
         this.enterButton.label = Locale.getById("extendedGUI.ConfirmWindow.okButton");
         this.tryCountLabel.text = Locale.getById("extendedGUI.ConfirmWindow.tryCounts") + " " + this.count;
         this.changeSendModeLabel.text = Locale.getById("extendedGUI.ConfirmWindow.changeConfirm");
         this.changeSendModeButton.label = code_type;
         this.refreshLabel.text = Locale.getById("extendedGUI.ConfirmWindow.resendText");
         this.updateRefreshButtonText();
         this.vBox.draw();
         Logger.LogToChannel(Logger.DEBUG,"ConfirmWindow",this.vBox.height,this.height);
         if(Math.round(this.vBox.height) != this.height)
         {
         }
         setTimeout(this.updateBtnPosition,10);
      }
      
      protected function updateBtnPosition() : void
      {
         if(this.changeSendModeLabel.textField.textWidth == 0)
         {
            setTimeout(this.updateBtnPosition,10);
            return;
         }
         this.changeSendModeLabel.width = this.changeSendModeLabel.textField.textWidth + 10;
      }
      
      protected function updateRefreshButtonText() : void
      {
         var result:* = "";
         var duration:Number = this.time - getTimer() / 1000;
         var _mins:uint = 0;
         var _secs:uint = 0;
         _mins = duration / 60 % 60;
         _secs = duration % 60;
         this.refreshButton.enabled = duration <= 0;
         if(duration > 0)
         {
            result = _secs.toString();
            if(_mins > 0)
            {
               if(_secs < 10)
               {
                  result = "0" + result;
               }
               result = _mins.toString() + ":" + result;
            }
            result = "(" + result + ")";
         }
         else
         {
            this.timer.stop();
         }
         this.refreshButton.label = Locale.getById("extendedGUI.ConfirmWindow.resendButton") + " " + result;
      }
      
      protected function timerTick(e:TimerEvent) : *
      {
         this.updateRefreshButtonText();
      }
      
      private function adjust(... args) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"ConfirmWindow adjust",this.vBox.width,this.vBox.height);
      }
   }
}

