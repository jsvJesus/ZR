package com.controls
{
   import com.communication.GameCommunication;
   import com.events.EmotionEvent;
   import flash.display.MovieClip;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.controls.ScrollingList;
   import scaleform.clik.data.DataProvider;
   import scaleform.clik.data.ListDataEmotion;
   
   public class EmotionList extends ScrollingList
   {
      public function EmotionList()
      {
         super();
         this.mouseChildren = true;
         this.mouseEnabled = true;
      }
      
      override public function set dataArray(value:Array) : void
      {
         var item:* = undefined;
         if(_dataArray == value)
         {
            return;
         }
         _dataArray = value;
         var tmp:Array = new Array();
         for each(item in _dataArray)
         {
            tmp.push(item.label);
         }
         this.dataProvider = new DataProvider(tmp);
      }
      
      override protected function populateData(data:Array) : void
      {
         var renderer:EmotionButton = null;
         var index:uint = 0;
         var listData:ListDataEmotion = null;
         var dl:uint = data.length;
         var l:uint = _renderers.length;
         for(var i:uint = 0; i < l; i++)
         {
            renderer = getRendererAt(i) as EmotionButton;
            index = _scrollPosition + i;
            if(data[i] != null)
            {
               listData = new ListDataEmotion(index,dataArray[index].id,itemToLabel(data[i]),dataArray[index].key_name,dataArray[index].key_id,false,dataArray[index].checked);
               renderer.enabled = i >= dl ? false : true;
               renderer.setListData(listData);
               renderer.label = itemToLabel(data[i]);
               renderer.key_name = dataArray[index].key_name || "";
               renderer.key_id = dataArray[index].key_id;
               renderer.id = dataArray[index].id;
               renderer.check = dataArray[index].checked;
               renderer.removeEventListener(EmotionEvent.CLICK,this.onMouseClick);
               renderer.addEventListener(EmotionEvent.CLICK,this.onMouseClick);
               renderer.removeEventListener(EmotionEvent.KEY_BIND,this.onKeyBind);
               renderer.addEventListener(EmotionEvent.KEY_BIND,this.onKeyBind);
               renderer.removeEventListener(EmotionEvent.CHECK,this.onCheckClick);
               renderer.addEventListener(EmotionEvent.CHECK,this.onCheckClick);
               renderer.validateNow();
            }
            else
            {
               renderer.enabled = false;
               renderer.label = "";
               renderer.key_name = "";
               renderer.removeEventListener(EmotionEvent.CLICK,this.onMouseClick);
               renderer.removeEventListener(EmotionEvent.KEY_BIND,this.onKeyBind);
               renderer.removeEventListener(EmotionEvent.CHECK,this.onCheckClick);
               renderer.validateNow();
            }
         }
      }
      
      override protected function updateScrollBar() : void
      {
         if(_scrollBar == null)
         {
            return;
         }
         var max:Number = Math.max(0,_dataProvider.length - _totalRenderers);
         _scrollBar.setScrollProperties(_dataProvider.length - _totalRenderers,0,_dataProvider.length - _totalRenderers);
         _scrollBar.position = _scrollPosition;
         _scrollBar.validateNow();
      }
      
      override protected function updateSelectedIndex() : void
      {
         if(_selectedIndex == _newSelectedIndex)
         {
            return;
         }
         if(_totalRenderers == 0)
         {
            return;
         }
         var renderer:MovieClip = getRendererAt(_selectedIndex,scrollPosition);
         if(renderer != null)
         {
            renderer.selected = false;
            renderer.validateNow();
            renderer.setState("up");
         }
         super.selectedIndex = _newSelectedIndex;
         if(_selectedIndex < 0 || _selectedIndex >= _dataProvider.length)
         {
            return;
         }
         renderer = getRendererAt(_selectedIndex,_scrollPosition);
         if(renderer != null)
         {
            renderer.selected = true;
            renderer.validateNow();
            renderer.setState("down");
         }
         else
         {
            scrollToIndex(_selectedIndex);
            renderer = getRendererAt(_selectedIndex,scrollPosition);
            renderer.selected = true;
            renderer.validateNow();
            renderer.setState("down");
         }
      }
      
      override protected function drawLayout() : void
      {
         var renderer:EmotionButton = null;
         var l:uint = _renderers.length;
         var h:Number = rowHeight;
         var w:Number = availableWidth - padding.horizontal;
         var rx:Number = margin + padding.left;
         var ry:Number = margin + padding.top;
         var dataWillChange:Boolean = isInvalid(InvalidationType.DATA);
         for(var i:uint = 0; i < l; i++)
         {
            renderer = getRendererAt(i) as EmotionButton;
            if(renderer != null)
            {
               renderer.x = 0;
               renderer.y = ry + i * h;
               renderer.height = h;
               renderer.width = w;
               renderer.enabled = false;
               renderer.label = "";
               renderer.key_name = "";
               renderer.setState("up");
               if(_dataArray.length > 0 && _dataArray[i] != null)
               {
                  renderer.check = dataArray[i].checked;
               }
               if(!dataWillChange)
               {
                  renderer.validateNow();
               }
            }
         }
         drawScrollBar();
         invalidateData();
      }
      
      protected function onMouseClick(e:EmotionEvent) : *
      {
         GameCommunication.python_trace("list CLICK " + e.sender.toString() + " " + e.index.toString());
         dispatchEvent(new EmotionEvent(EmotionEvent.CLICK,e.sender,e.id,e.check,e.index));
      }
      
      protected function onKeyBind(e:EmotionEvent) : *
      {
         GameCommunication.python_trace("list KEY_BIND " + e.sender.toString() + " " + e.index.toString());
         dispatchEvent(new EmotionEvent(EmotionEvent.KEY_BIND,e.sender,e.id,e.check,e.index));
      }
      
      protected function onCheckClick(e:EmotionEvent) : *
      {
         dataArray[e.index].checked = e.check;
         GameCommunication.python_trace("list CHECK " + e.sender.toString() + " " + e.index.toString());
         dispatchEvent(new EmotionEvent(EmotionEvent.CHECK,e.sender,e.id,e.check,e.index));
      }
   }
}

