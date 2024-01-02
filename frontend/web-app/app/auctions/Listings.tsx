'use client'
import React, { useEffect, useState } from 'react'
import Auctioncard from './Auctioncard';
import AppPagination from '../components/AppPagination';
import { getData } from '../actions/auctionActions';
import Filters from './Filters';
import {shallow} from 'zustand/shallow';
import { useParamsStore } from '@/hooks/useParamStore';
import qs from 'query-string';
import EmptyFilter from '../components/EmptyFilter';
import { useAuctionStore } from '@/hooks/useAuctionStore';


export default  function Listings() {

  //const[data, setData] = useState<PagedResult<Auction>>();
  const [loading, setLoading] = useState(true);
  const params = useParamsStore(state => ({
    pageNumber: state.pageNumber,
    pageSize: state.pageSize,
    searchTerm: state.searchTerm,
    orderBy: state.orderBy,
    filterBy: state.filterBy,
    seller: state.seller,
    winner: state.winner

  }), shallow)

  
  const data = useAuctionStore(state =>({
    auctions: state.auctions,
    totalCount: state.totalCount,
    pageCount: state.pageCount

  }), shallow)

  const setData = useAuctionStore(state => state.setData);

  const setParams = useParamsStore(state => state.setParams);
  const url = qs.stringifyUrl({ url: '', query: params })

  function setPageNumber(pageNumber: number) {
        setParams({ pageNumber })
  }


  /* used zustand state management
  const [auctions,setAuctions] = useState<Auction[]>([]);
  const [pageCount,setPageCount] = useState(0);
  const [pageNumber,setPageNumber]= useState(1);
  const [pageSize, setPageSize] = useState(4);
  */
  

  useEffect(() => {
    getData(url).then(data => {
      setData(data);
      setLoading(false);

    })
  }, [url])

  //if(!data) return <h3>loading...</h3>
  if(loading) return <h3>loading...</h3>
  

  return (
        <>
            <Filters />
            {data.totalCount === 0 ? (
                <EmptyFilter showReset />
            ) : (
                <>
                    <div className='grid grid-cols-4 gap-6'>
                        {data.auctions.map(auction => (
                            <Auctioncard auction={auction} key={auction.id} />
                        ))}
                    </div>
                    <div className='flex justify-center mt-4'>
                        <AppPagination pageChanged={setPageNumber}
                            currentPage={params.pageNumber} pageCount={data.pageCount} />
                    </div>
                </>
            )}

        </>
  
  )
}
