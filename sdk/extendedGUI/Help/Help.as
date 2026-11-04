package
{
   import com.communication.ImageLoader;
   import com.communication.Localization;
   import com.communication.parseCards;
   import com.controls.Cards;
   import com.events.helpEvent;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.FocusEvent;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.external.ExternalInterface;
   import flash.ui.Keyboard;
   import flash.utils.setTimeout;
   
   public class Help extends MovieClip
   {
      public var Wi:Widget;
      
      public var MainHelpWindow:FormHelp;
      
      public function Help()
      {
         super();
         ExternalInterface.addCallback("localized_resource",this.localizate);
         ExternalInterface.addCallback("load_images_paths",this.loadImages);
         ExternalInterface.addCallback("load_help_cards",this.loadCards);
         ExternalInterface.addCallback("show_card",this.show_card);
         ExternalInterface.addCallback("close_card",this.close_card);
         ExternalInterface.addCallback("show_paragraph",this.show_paragraph);
         ExternalInterface.addCallback("show_help",this.onShowHelp);
         ExternalInterface.addCallback("hide_help",this.onHideHelp);
         setTimeout(ExternalInterface.call,1,"load_images_paths");
         setTimeout(ExternalInterface.call,1,"localized_resource",{"paths":["Help"]});
         setTimeout(ExternalInterface.call,1,"load_help_cards");
         stage.addEventListener(Event.RESIZE,this.onResizeStage);
         stage.addEventListener(MouseEvent.CLICK,this.onMouseClick);
         this.MainHelpWindow.find.addEventListener(KeyboardEvent.KEY_DOWN,this.inputInFind);
         this.MainHelpWindow.find.addEventListener(FocusEvent.FOCUS_IN,this.onInputFocus);
         this.MainHelpWindow.find.addEventListener(FocusEvent.FOCUS_OUT,this.onOutputFocus);
         this.MainHelpWindow.list.addEventListener(helpEvent.SELECT,this.onParagraphSelected);
         this.MainHelpWindow.closeBtn.addEventListener(MouseEvent.CLICK,this.onCloseHelp);
         this.MainHelpWindow.visible = false;
         this.MainHelpWindow.head.addEventListener(MouseEvent.MOUSE_DOWN,this.onWindowStartDrag);
         this.Wi.Hide();
         this.Wi.visible = false;
      }
      
      internal function onParagraphSelected(param1:helpEvent) : *
      {
         this.MainHelpWindow.content.setCard(param1.index);
      }
      
      protected function onWindowStartDrag(param1:MouseEvent) : *
      {
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onWindowStopDrag,false,0,true);
         this.MainHelpWindow.startDrag();
      }
      
      protected function onWindowStopDrag(param1:Event) : void
      {
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onWindowStopDrag,false);
         this.MainHelpWindow.stopDrag();
      }
      
      protected function validatePosition() : *
      {
         if(this.MainHelpWindow.y < (Object(root).height - stage.stageHeight) / 2)
         {
            this.MainHelpWindow.y = (Object(root).height - stage.stageHeight) / 2;
         }
         if(this.MainHelpWindow.x < (Object(root).width - stage.stageWidth) / 2)
         {
            this.MainHelpWindow.x = (Object(root).width - stage.stageWidth) / 2;
         }
         if(this.MainHelpWindow.y > (Object(root).height + stage.stageHeight) / 2 - 40)
         {
            this.MainHelpWindow.y = (Object(root).height + stage.stageHeight) / 2 - 70;
         }
         if(this.MainHelpWindow.x > (Object(root).width + stage.stageWidth) / 2 - 101)
         {
            this.MainHelpWindow.x = (Object(root).width + stage.stageWidth) / 2 - 101;
         }
      }
      
      protected function onCloseHelp(param1:MouseEvent) : *
      {
         ExternalInterface.call("close_help");
      }
      
      protected function onHideHelp(param1:*) : *
      {
         stage.focus = null;
         this.MainHelpWindow.visible = false;
      }
      
      protected function onShowHelp(param1:*) : *
      {
         stage.focus = null;
         this.MainHelpWindow.visible = true;
      }
      
      protected function localizate(param1:*) : *
      {
         Localization.setLocalData(param1.Help);
         this.MainHelpWindow.Exit_label.htmlText = Localization.getLocal("CLOSE_HELP");
         this.MainHelpWindow.Contents_label.htmlText = Localization.getLocal("CONTENTS");
         this.Wi.onGetLocal();
      }
      
      protected function loadImages(param1:*) : *
      {
         var Obj:* = param1;
         try
         {
            ImageLoader.LoadImages(Obj);
         }
         catch(e:Error)
         {
         }
      }
      
      protected function loadCards(param1:*) : *
      {
         this.MainHelpWindow.list.data(parseCards.getContents(param1.chapters));
      }
      
      protected function onResizeStage(param1:Event) : *
      {
         this.Wi.x = ((Object(root).width - stage.stageWidth) / 2 + stage.stageWidth - this.Wi.width) / 2;
      }
      
      protected function inputInFind(param1:KeyboardEvent) : *
      {
         if(param1.keyCode == Keyboard.ENTER || param1.keyCode == Keyboard.NUMPAD_ENTER)
         {
            ExternalInterface.call("find_card",this.MainHelpWindow.find.text);
         }
      }
      
      protected function onInputFocus(param1:FocusEvent) : *
      {
         ExternalInterface.call("on_cnange_input_focus",{"value":true});
      }
      
      protected function onOutputFocus(param1:FocusEvent) : *
      {
         ExternalInterface.call("on_cnange_input_focus",{"value":false});
      }
      
      protected function show_card(param1:Number) : *
      {
         this.Wi.addCard(Cards.getLittleCardById(param1));
      }
      
      protected function show_paragraph(param1:Number) : *
      {
         this.MainHelpWindow.content.setCard(param1);
         this.MainHelpWindow.visible = true;
      }
      
      protected function onMouseClick(param1:MouseEvent) : *
      {
         if(param1.eventPhase == 2)
         {
            stage.focus = null;
         }
      }
      
      protected function close_card() : *
      {
         this.Wi.Hide();
      }
   }
}

